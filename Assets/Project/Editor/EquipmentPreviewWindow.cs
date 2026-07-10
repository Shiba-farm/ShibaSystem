using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Equipment Preview Window — Tools > Shiba Farm > Equipment Preview (Ctrl+Shift+E)
///
/// รองรับ 2 โหมด:
///   • Hand-held mode  : แสดง equipmentPrefab ที่ RightHandAnchor / LiftAnchor
///                       แก้ไข HoldPositions (Idle / Acting)
///   • Wearable mode   : ตรวจจับ WearableItemSO อัตโนมัติ
///                       แสดง visualPrefab ที่ bone anchor จาก PlayerWearableVisual
///                       แก้ไข visualPositionOffset / visualRotationOffset / visualScale
///                       ลาก Gizmo ใน Scene View เพื่อ adjust ตำแหน่งได้ทันที
/// </summary>
public class EquipmentPreviewWindow : EditorWindow
{
    // ─── Setup ────────────────────────────────────────────────────────────────
    private GameObject  _playerRoot;
    private ItemSO      _selectedItem;

    // ─── Preview ──────────────────────────────────────────────────────────────
    private GameObject  _previewInstance;
    private Transform   _handAnchor;
    private HoldState   _editingState = HoldState.Idle;

    // ─── Serialization — Hold mode ────────────────────────────────────────────
    private SerializedObject   _serializedItem;
    private SerializedProperty _holdPositionsProp;

    // ─── Serialization — Wearable mode ───────────────────────────────────────
    private bool               _isWearableMode;
    private SerializedProperty _visualPosProp;
    private SerializedProperty _visualRotProp;
    private SerializedProperty _visualScaleProp;

    // ─── Animation ────────────────────────────────────────────────────────────
    private List<AnimationClip> _clips = new();
    private int   _selectedClipIndex  = -1;
    private float _animNormalizedTime;
    private bool  _isPlaying;
    private double _lastEditorTime;
    private bool  _ownedAnimMode;

    // ─── UI ───────────────────────────────────────────────────────────────────
    private Vector2 _scroll;

    // ─── Static instance (for ItemSO.OnValidate bridge) ──────────────────────
    private static EquipmentPreviewWindow _instance;

    // ─────────────────────────────────────────────────────────────────────────
    // MENU ENTRY
    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Shiba Farm/Equipment Preview %#e")]
    public static void Open()
    {
        var win = GetWindow<EquipmentPreviewWindow>("⚔ Equipment Preview");
        win.minSize = new Vector2(340, 540);
        win.Show();
    }

    public static void OnItemSOChanged(ItemSO item)
    {
        if (_instance == null || _instance._selectedItem != item) return;
        if (_instance._previewInstance == null) return;
        _instance._serializedItem?.Update();
        _instance.ApplyCurrentOffsets();
        _instance.Repaint();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        _instance = this;
        EditorApplication.update          += EditorUpdate;
        Undo.undoRedoPerformed            += OnUndoRedo;
        ItemPreviewBridge.OnItemSOChanged += OnItemSOChanged;
        SceneView.duringSceneGui          += OnSceneGUI;

        if (_playerRoot == null)
            TryFindPlayer();

        if (_selectedItem != null)
            BindSerializedItem(_selectedItem);

        RefreshClips();
    }

    private void OnDisable()
    {
        _instance = null;
        EditorApplication.update          -= EditorUpdate;
        Undo.undoRedoPerformed            -= OnUndoRedo;
        ItemPreviewBridge.OnItemSOChanged -= OnItemSOChanged;
        SceneView.duringSceneGui          -= OnSceneGUI;

        DestroyPreview();
    }

    private void OnSelectionChange()
    {
        if (Selection.activeObject is ItemSO so && so != _selectedItem)
        {
            SetItem(so);
            Repaint();
        }
    }

    private void OnProjectChange() => RefreshClips();

    private void OnUndoRedo()
    {
        if (_selectedItem == null) return;
        _serializedItem?.Update();
        ApplyCurrentOffsets();
        Repaint();
    }

    private void EditorUpdate()
    {
        WriteBackIfDirty();

        if (!_isPlaying || _playerRoot == null) return;
        if (_selectedClipIndex < 0 || _selectedClipIndex >= _clips.Count) return;
        var clip = _clips[_selectedClipIndex];
        if (clip == null) return;

        double now   = EditorApplication.timeSinceStartup;
        float  delta = (float)(now - _lastEditorTime);
        _lastEditorTime = now;

        _animNormalizedTime += delta / clip.length;
        if (_animNormalizedTime > 1f) _animNormalizedTime -= 1f;

        SampleAnimation();
        Repaint();
    }

    private void OnSceneGUI(SceneView _) => WriteBackIfDirty();

    /// <summary>
    /// Detect gizmo drag in Scene View and write back to the SO.
    /// Wearable mode writes to visualPositionOffset/Rotation/Scale.
    /// Hold mode writes to holdPositions[state].
    /// </summary>
    private void WriteBackIfDirty()
    {
        if (_previewInstance == null || _selectedItem == null || _serializedItem == null) return;

        var t = _previewInstance.transform;

        if (_isWearableMode && _selectedItem is WearableItemSO wearable)
        {
            bool posChanged   = (t.localPosition    - wearable.visualPositionOffset).sqrMagnitude > 1e-6f;
            bool rotChanged   = (WrapEuler(t.localEulerAngles) - WrapEuler(wearable.visualRotationOffset)).sqrMagnitude > 0.01f;
            var  storedScale  = wearable.visualScale == Vector3.zero ? Vector3.one : wearable.visualScale;
            bool scaleChanged = (t.localScale        - storedScale).sqrMagnitude > 1e-6f;

            if (!posChanged && !rotChanged && !scaleChanged) return;

            _serializedItem.Update();
            if (_visualPosProp   != null) _visualPosProp.vector3Value   = t.localPosition;
            if (_visualRotProp   != null) _visualRotProp.vector3Value   = t.localEulerAngles;
            if (_visualScaleProp != null) _visualScaleProp.vector3Value = t.localScale;
            _serializedItem.ApplyModifiedProperties();

            Repaint();
        }
        else
        {
            var hold = _selectedItem.GetHoldPosition(_editingState);
            if (hold == null) return;

            bool posChanged   = (t.localPosition    - hold.positionOffset).sqrMagnitude > 1e-6f;
            bool rotChanged   = (WrapEuler(t.localEulerAngles) - WrapEuler(hold.rotationOffset)).sqrMagnitude > 0.01f;
            var  sceneScale   = t.localScale == Vector3.zero ? Vector3.one : t.localScale;
            bool scaleChanged = (sceneScale          - hold.localScale).sqrMagnitude > 1e-6f;

            if (!posChanged && !rotChanged && !scaleChanged) return;

            _serializedItem.Update();
            int idx = GetOrCreateHoldPositionIndex(_editingState);
            if (idx < 0) return;

            var elem = _holdPositionsProp.GetArrayElementAtIndex(idx);
            elem.FindPropertyRelative("positionOffset").vector3Value = t.localPosition;
            elem.FindPropertyRelative("rotationOffset").vector3Value = t.localEulerAngles;
            var scaleProp = elem.FindPropertyRelative("localScale");
            if (scaleProp != null) scaleProp.vector3Value = t.localScale;
            _serializedItem.ApplyModifiedProperties();

            Repaint();
        }
    }

    private static Vector3 WrapEuler(Vector3 e)
    {
        return new Vector3(
            e.x < 0f ? e.x + 360f : e.x,
            e.y < 0f ? e.y + 360f : e.y,
            e.z < 0f ? e.z + 360f : e.z);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MAIN GUI
    // ─────────────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        if (_playerRoot != null && !_playerRoot)
        {
            _playerRoot = null;
            DestroyPreview();
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawHeader();
        DrawDivider();
        DrawSetupSection();
        DrawDivider();
        DrawOffsetSection();

        if (_previewInstance != null)
        {
            DrawDivider();
            DrawAnimationSection();
        }

        EditorGUILayout.EndScrollView();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HEADER
    // ─────────────────────────────────────────────────────────────────────────

    private void DrawHeader()
    {
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 14,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(0.9f, 0.85f, 0.5f) }
        };
        GUILayout.Space(8);
        EditorGUILayout.LabelField("⚔  Equipment Preview", style, GUILayout.Height(26));
        GUILayout.Space(4);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SETUP SECTION
    // ─────────────────────────────────────────────────────────────────────────

    private void DrawSetupSection()
    {
        DrawSectionLabel("SETUP");

        // ── Player row ──
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();
            var newPlayer = (GameObject)EditorGUILayout.ObjectField(
                "Player", _playerRoot, typeof(GameObject), allowSceneObjects: true);
            if (EditorGUI.EndChangeCheck())
            {
                _playerRoot = newPlayer;
                DestroyPreview();
            }

            if (GUILayout.Button("Find", GUILayout.Width(44)))
                TryFindPlayer();
        }

        // ── Item row ──
        EditorGUI.BeginChangeCheck();
        var newItem = (ItemSO)EditorGUILayout.ObjectField(
            "Item", _selectedItem, typeof(ItemSO), allowSceneObjects: false);
        if (EditorGUI.EndChangeCheck())
            SetItem(newItem);

        // ── Mode indicator ──
        if (_selectedItem != null)
        {
            if (_isWearableMode && _selectedItem is WearableItemSO w)
            {
                var modeStyle = new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = new Color(0.5f, 0.9f, 0.6f) } };
                EditorGUILayout.LabelField($"  🎽 Wearable mode  |  Slot: {w.slot}", modeStyle);
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.EnumPopup("  Hold Type", _selectedItem.holdType);
            }
        }

        GUILayout.Space(6);

        // ── Preview buttons ──
        bool hasPreview = _previewInstance != null;
        bool canStart   = CanStartPreview();

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = canStart;
            string label = hasPreview ? "↺  Refresh Preview" : "▶  Start Preview";
            if (GUILayout.Button(label, GUILayout.Height(26)))
                StartPreview();

            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.38f, 0.38f);
            GUI.enabled = hasPreview;
            if (GUILayout.Button("✕  Stop", GUILayout.Width(66), GUILayout.Height(26)))
                DestroyPreview();
            GUI.backgroundColor = oldBg;
            GUI.enabled = true;
        }

        // ── Status / Help ──
        GUILayout.Space(2);
        if (hasPreview)
        {
            string anchorName = _handAnchor?.name ?? "?";
            EditorGUILayout.HelpBox(
                $"✔  '{_selectedItem.itemName}'  →  {anchorName}",
                MessageType.None);

            if (GUILayout.Button("🎯  Select Preview Item in Scene", GUILayout.Height(24)))
            {
                Selection.activeGameObject = _previewInstance;
                SceneView.lastActiveSceneView?.FrameSelected();
            }
        }
        else if (_selectedItem != null)
        {
            string msg = GetCannotStartReason();
            if (!string.IsNullOrEmpty(msg))
                EditorGUILayout.HelpBox(msg, MessageType.Warning);
        }
    }

    private bool CanStartPreview()
    {
        if (_selectedItem == null || _playerRoot == null) return false;

        if (_isWearableMode)
            return _selectedItem is WearableItemSO w && w.visualPrefab != null;

        return _selectedItem.holdType != HoldType.None
            && _selectedItem.equipmentPrefab != null;
    }

    private string GetCannotStartReason()
    {
        if (_playerRoot == null) return "Assign the Player scene object or click Find.";

        if (_isWearableMode)
        {
            if (_selectedItem is WearableItemSO w && w.visualPrefab == null)
                return "WearableItemSO ไม่มี Visual Prefab — กำหนดใน Inspector ของ SO";
            return "";
        }

        if (_selectedItem.holdType == HoldType.None)
            return "HoldType is None — no visual in hand.";
        if (_selectedItem.equipmentPrefab == null)
            return "No equipmentPrefab assigned on this ItemSO.";

        return "";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // OFFSET SECTION
    // ─────────────────────────────────────────────────────────────────────────

    private void DrawOffsetSection()
    {
        if (_isWearableMode)
            DrawWearableOffsetSection();
        else
            DrawHoldOffsetSection();
    }

    // ── Wearable Offset UI ────────────────────────────────────────────────────

    private void DrawWearableOffsetSection()
    {
        DrawSectionLabel("WEARABLE VISUAL OFFSET");

        if (_serializedItem == null)
        {
            EditorGUILayout.HelpBox("Select a WearableItemSO to edit its visual offset.", MessageType.None);
            return;
        }

        if (_visualPosProp == null || _visualRotProp == null || _visualScaleProp == null)
        {
            EditorGUILayout.HelpBox(
                "ไม่พบ visualPositionOffset / visualRotationOffset / visualScale\n" +
                "ตรวจสอบว่า WearableItemSO.cs มี field เหล่านี้แล้ว Re-import",
                MessageType.Error);
            return;
        }

        _serializedItem.Update();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_visualPosProp,   new GUIContent("Position Offset"));
        EditorGUILayout.PropertyField(_visualRotProp,   new GUIContent("Rotation Offset"));
        EditorGUILayout.PropertyField(_visualScaleProp, new GUIContent("Scale"));
        bool changed = EditorGUI.EndChangeCheck();

        _serializedItem.ApplyModifiedProperties();

        if (changed && _previewInstance != null)
            ApplyCurrentOffsets();

        GUILayout.Space(4);

        // ── Reset row ──
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("↺ Position"))  ResetWearableVec3(_visualPosProp,   Vector3.zero);
            if (GUILayout.Button("↺ Rotation"))  ResetWearableVec3(_visualRotProp,   Vector3.zero);
            if (GUILayout.Button("↺ Scale"))      ResetWearableVec3(_visualScaleProp, Vector3.one);
        }

        GUILayout.Space(4);

        EditorGUILayout.HelpBox(
            "💡 คลิก 'Select Preview Item in Scene' แล้วใช้ Move/Rotate/Scale tool ใน Scene View\n" +
            "    ค่าจะ sync กลับมาที่ SO อัตโนมัติขณะลาก",
            MessageType.None);
    }

    private void ResetWearableVec3(SerializedProperty prop, Vector3 value)
    {
        if (prop == null) return;
        _serializedItem.Update();
        prop.vector3Value = value;
        _serializedItem.ApplyModifiedProperties();
        if (_previewInstance != null) ApplyCurrentOffsets();
    }

    // ── Hold Offset UI (เดิม) ─────────────────────────────────────────────────

    private void DrawHoldOffsetSection()
    {
        DrawSectionLabel("OFFSETS");

        if (_serializedItem == null)
        {
            EditorGUILayout.HelpBox("Select an ItemSO to edit its hold offsets.", MessageType.None);
            return;
        }

        _serializedItem.Update();

        // ── HoldState toggle ──
        EditorGUI.BeginChangeCheck();
        _editingState = (HoldState)GUILayout.Toolbar(
            (int)_editingState,
            System.Enum.GetNames(typeof(HoldState)),
            GUILayout.Height(22));
        if (EditorGUI.EndChangeCheck())
            ApplyCurrentOffsets();

        GUILayout.Space(6);

        // ── Offset fields ──
        int idx = GetOrCreateHoldPositionIndex(_editingState);
        if (idx < 0)
        {
            EditorGUILayout.HelpBox("Failed to resolve HoldPosition entry.", MessageType.Error);
            return;
        }

        var elemProp  = _holdPositionsProp.GetArrayElementAtIndex(idx);
        var posProp   = elemProp.FindPropertyRelative("positionOffset");
        var rotProp   = elemProp.FindPropertyRelative("rotationOffset");
        var scaleProp = elemProp.FindPropertyRelative("localScale");

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(posProp,   new GUIContent("Position"));
        EditorGUILayout.PropertyField(rotProp,   new GUIContent("Rotation"));
        if (scaleProp != null)
            EditorGUILayout.PropertyField(scaleProp, new GUIContent("Scale"));
        bool changed = EditorGUI.EndChangeCheck();

        _serializedItem.ApplyModifiedProperties();

        if (changed && _previewInstance != null)
            ApplyCurrentOffsets();

        GUILayout.Space(4);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("↺ Position"))
                ResetVec3Property(posProp, Vector3.zero);
            if (GUILayout.Button("↺ Rotation"))
                ResetVec3Property(rotProp, Vector3.zero);
            if (scaleProp != null && GUILayout.Button("↺ Scale"))
                ResetVec3Property(scaleProp, Vector3.one);
        }

        GUILayout.Space(6);

        DrawSectionLabel("COPY STATE");
        foreach (HoldState st in System.Enum.GetValues(typeof(HoldState)))
        {
            if (st == _editingState) continue;
            if (GUILayout.Button($"Copy  {_editingState}  →  {st}"))
                CopyStateOffsets(_editingState, st);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ANIMATION SECTION
    // ─────────────────────────────────────────────────────────────────────────

    private void DrawAnimationSection()
    {
        DrawSectionLabel("ANIMATION PREVIEW");

        if (_clips.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No clips found in Assets/Project/Animations/Characters/Player.\n" +
                "Click ↺ Refresh or drag a clip into the field below.",
                MessageType.Warning);
        }
        else
        {
            string[] names = _clips.Select(c => c != null ? c.name : "(null)").ToArray();
            EditorGUI.BeginChangeCheck();
            int newIdx = EditorGUILayout.Popup(
                "Clip", Mathf.Max(_selectedClipIndex, 0), names);
            if (EditorGUI.EndChangeCheck())
            {
                _selectedClipIndex   = newIdx;
                _animNormalizedTime  = 0f;
                EnsureAnimMode();
                SampleAnimation();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PrefixLabel("Add Clip");
            var dragged = (AnimationClip)EditorGUILayout.ObjectField(
                null, typeof(AnimationClip), allowSceneObjects: false);
            if (dragged != null && !_clips.Contains(dragged))
            {
                _clips.Add(dragged);
                _selectedClipIndex  = _clips.Count - 1;
                _animNormalizedTime = 0f;
                EnsureAnimMode();
                SampleAnimation();
            }
        }

        GUILayout.Space(4);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("|◀", GUILayout.Width(30)))
            {
                _animNormalizedTime = 0f;
                EnsureAnimMode();
                SampleAnimation();
            }

            var oldBg = GUI.backgroundColor;
            if (_isPlaying)
            {
                GUI.backgroundColor = new Color(1f, 0.70f, 0.20f);
                if (GUILayout.Button("⏸  Pause")) _isPlaying = false;
            }
            else
            {
                GUI.backgroundColor = new Color(0.35f, 0.85f, 0.45f);
                if (GUILayout.Button("▶  Play"))
                {
                    _isPlaying      = true;
                    _lastEditorTime = EditorApplication.timeSinceStartup;
                    EnsureAnimMode();
                }
            }
            GUI.backgroundColor = oldBg;

            if (GUILayout.Button("■  Stop", GUILayout.Width(64)))
            {
                _isPlaying          = false;
                _animNormalizedTime = 0f;
                StopAnimMode();
                Repaint();
            }
        }

        if (_selectedClipIndex >= 0 && _selectedClipIndex < _clips.Count)
        {
            var clip = _clips[_selectedClipIndex];
            if (clip != null)
            {
                EditorGUI.BeginChangeCheck();
                float newNorm = EditorGUILayout.Slider(
                    $"{_animNormalizedTime * clip.length:F2}s / {clip.length:F2}s",
                    _animNormalizedTime, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    _animNormalizedTime = newNorm;
                    EnsureAnimMode();
                    SampleAnimation();
                }
            }
        }

        GUILayout.Space(4);

        if (_clips.Count > 1)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("◀  Prev Clip"))
                {
                    _selectedClipIndex  = (_selectedClipIndex - 1 + _clips.Count) % _clips.Count;
                    _animNormalizedTime = 0f;
                    SampleAnimation();
                }
                if (GUILayout.Button("Next Clip  ▶"))
                {
                    _selectedClipIndex  = (_selectedClipIndex + 1) % _clips.Count;
                    _animNormalizedTime = 0f;
                    SampleAnimation();
                }
            }
        }

        GUILayout.Space(2);
        if (GUILayout.Button("↺  Refresh Clip List"))
            RefreshClips();

        if (AnimationMode.InAnimationMode())
        {
            GUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Animation Mode active — player pose is driven by the preview clip.",
                MessageType.None);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PREVIEW MANAGEMENT
    // ─────────────────────────────────────────────────────────────────────────

    private void StartPreview()
    {
        DestroyPreview();

        if (_selectedItem == null || _playerRoot == null) return;

        if (_isWearableMode && _selectedItem is WearableItemSO wearable)
        {
            // ── Wearable mode: ใช้ visualPrefab ที่ bone anchor ──────────────
            if (wearable.visualPrefab == null) return;

            _handAnchor = ResolveWearableAnchor(wearable.slot);
            if (_handAnchor == null)
            {
                Debug.LogWarning(
                    $"[EquipmentPreview] ไม่พบ anchor สำหรับ slot {wearable.slot}\n" +
                    "ตรวจสอบว่า PlayerWearableVisual มีค่า Slot Anchors ครบ");
                return;
            }
        }
        else
        {
            // ── Hold mode: ใช้ equipmentPrefab ที่ RightHandAnchor ─────────
            if (_selectedItem.holdType == HoldType.None || _selectedItem.equipmentPrefab == null) return;

            _handAnchor = ResolveAnchor(_selectedItem.holdType);
            if (_handAnchor == null)
            {
                Debug.LogWarning(
                    "[EquipmentPreview] Could not find RightHandAnchor or LiftAnchor " +
                    "in the player hierarchy.");
                return;
            }
        }

        // ── Spawn preview instance ────────────────────────────────────────────
        var prefab = (_isWearableMode && _selectedItem is WearableItemSO w2)
            ? w2.visualPrefab
            : _selectedItem.equipmentPrefab;

        _previewInstance           = Instantiate(prefab, _handAnchor);
        _previewInstance.hideFlags = HideFlags.DontSave;
        _previewInstance.name      = $"[Preview] {_selectedItem.itemName}";

        ApplyCurrentOffsets();
        SceneView.RepaintAll();
    }

    private void DestroyPreview()
    {
        _isPlaying = false;
        StopAnimMode();

        if (_previewInstance != null)
        {
            DestroyImmediate(_previewInstance);
            _previewInstance = null;
        }
        _handAnchor = null;
        SceneView.RepaintAll();
    }

    private void ApplyCurrentOffsets()
    {
        if (_previewInstance == null || _selectedItem == null) return;

        if (_isWearableMode && _selectedItem is WearableItemSO wearable)
        {
            _previewInstance.transform.localPosition    = wearable.visualPositionOffset;
            _previewInstance.transform.localEulerAngles = wearable.visualRotationOffset;
            var scale = wearable.visualScale;
            _previewInstance.transform.localScale = scale == Vector3.zero ? Vector3.one : scale;
        }
        else
        {
            var hold = _selectedItem.GetHoldPosition(_editingState);
            if (hold == null) return;
            _previewInstance.transform.localPosition    = hold.positionOffset;
            _previewInstance.transform.localEulerAngles = hold.rotationOffset;
            _previewInstance.transform.localScale =
                hold.localScale == Vector3.zero ? Vector3.one : hold.localScale;
        }

        SceneView.RepaintAll();
    }

    // ── Anchor Resolution ─────────────────────────────────────────────────────

    /// <summary>
    /// หา bone anchor สำหรับ wearable จาก PlayerWearableVisual บน player
    /// ใช้ GetAnchor(slot) ที่ exposed เป็น public method
    /// </summary>
    private Transform ResolveWearableAnchor(EquipSlot slot)
    {
        var wearableVisual = _playerRoot.GetComponent<PlayerWearableVisual>()
                          ?? _playerRoot.GetComponentInChildren<PlayerWearableVisual>();

        if (wearableVisual == null)
        {
            Debug.LogWarning(
                "[EquipmentPreview] ไม่พบ PlayerWearableVisual บน Player — " +
                "เพิ่ม component และตั้งค่า Slot Anchors ก่อน");
            return null;
        }

        return wearableVisual.GetAnchor(slot);
    }

    private Transform ResolveAnchor(HoldType holdType)
    {
        string name = holdType == HoldType.TwoHandLift ? "LiftAnchor" : "RightHandAnchor";
        return FindDeep(_playerRoot.transform, name);
    }

    private static Transform FindDeep(Transform root, string targetName)
    {
        if (root.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase))
            return root;
        foreach (Transform child in root)
        {
            var hit = FindDeep(child, targetName);
            if (hit != null) return hit;
        }
        return null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SERIALIZED OBJECT HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private void SetItem(ItemSO item)
    {
        DestroyPreview();
        _selectedItem      = item;
        _serializedItem    = null;
        _holdPositionsProp = null;
        _visualPosProp     = null;
        _visualRotProp     = null;
        _visualScaleProp   = null;
        _isWearableMode    = item is WearableItemSO;

        if (_selectedItem != null)
            BindSerializedItem(_selectedItem);
    }

    private void BindSerializedItem(ItemSO item)
    {
        _isWearableMode    = item is WearableItemSO;
        _serializedItem    = new SerializedObject(item);
        _holdPositionsProp = _serializedItem.FindProperty("holdPositions");

        if (_isWearableMode)
        {
            _visualPosProp   = _serializedItem.FindProperty("visualPositionOffset");
            _visualRotProp   = _serializedItem.FindProperty("visualRotationOffset");
            _visualScaleProp = _serializedItem.FindProperty("visualScale");
        }
        else
        {
            EnsureAllStatesExist();
        }
    }

    private void EnsureAllStatesExist()
    {
        if (_selectedItem == null || _isWearableMode) return;
        bool dirty = false;

        foreach (HoldState s in System.Enum.GetValues(typeof(HoldState)))
        {
            if (_selectedItem.holdPositions.Find(h => h.state == s) != null) continue;

            Undo.RecordObject(_selectedItem, "Init HoldPosition");
            _selectedItem.holdPositions.Add(new HoldPosition
            {
                state          = s,
                positionOffset = Vector3.zero,
                rotationOffset = Vector3.zero,
                localScale     = Vector3.one
            });
            dirty = true;
        }

        if (!dirty) return;

        EditorUtility.SetDirty(_selectedItem);
        _serializedItem    = new SerializedObject(_selectedItem);
        _holdPositionsProp = _serializedItem.FindProperty("holdPositions");
    }

    private int GetOrCreateHoldPositionIndex(HoldState state)
    {
        if (_holdPositionsProp == null) return -1;
        _serializedItem.Update();

        for (int i = 0; i < _holdPositionsProp.arraySize; i++)
        {
            var sp = _holdPositionsProp.GetArrayElementAtIndex(i).FindPropertyRelative("state");
            if (sp != null && sp.intValue == (int)state) return i;
        }

        EnsureAllStatesExist();
        _serializedItem.Update();

        for (int i = 0; i < _holdPositionsProp.arraySize; i++)
        {
            var sp = _holdPositionsProp.GetArrayElementAtIndex(i).FindPropertyRelative("state");
            if (sp != null && sp.intValue == (int)state) return i;
        }

        return -1;
    }

    private void ResetVec3Property(SerializedProperty prop, Vector3 value)
    {
        _serializedItem.Update();
        prop.vector3Value = value;
        _serializedItem.ApplyModifiedProperties();
        if (_previewInstance != null) ApplyCurrentOffsets();
    }

    private void CopyStateOffsets(HoldState from, HoldState to)
    {
        if (_selectedItem == null) return;

        Undo.RecordObject(_selectedItem, $"Copy {from} → {to}");
        var src = _selectedItem.GetHoldPosition(from);
        var dst = _selectedItem.GetHoldPosition(to);
        if (src == null || dst == null) return;

        dst.positionOffset = src.positionOffset;
        dst.rotationOffset = src.rotationOffset;
        dst.localScale     = src.localScale;

        EditorUtility.SetDirty(_selectedItem);
        _serializedItem    = new SerializedObject(_selectedItem);
        _holdPositionsProp = _serializedItem.FindProperty("holdPositions");

        ApplyCurrentOffsets();
        Repaint();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ANIMATION MODE
    // ─────────────────────────────────────────────────────────────────────────

    private void EnsureAnimMode()
    {
        if (AnimationMode.InAnimationMode()) return;
        AnimationMode.StartAnimationMode();
        _ownedAnimMode = true;
    }

    private void StopAnimMode()
    {
        if (_ownedAnimMode && AnimationMode.InAnimationMode())
            AnimationMode.StopAnimationMode();
        _ownedAnimMode = false;
        ApplyCurrentOffsets();
    }

    private void SampleAnimation()
    {
        if (_playerRoot == null) return;
        if (_selectedClipIndex < 0 || _selectedClipIndex >= _clips.Count) return;
        var clip = _clips[_selectedClipIndex];
        if (clip == null) return;

        var animRoot = GetAnimatorRoot();
        if (animRoot == null) return;

        EnsureAnimMode();
        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(animRoot, clip, _animNormalizedTime * clip.length);
        AnimationMode.EndSampling();

        SceneView.RepaintAll();
    }

    private GameObject GetAnimatorRoot()
    {
        if (_playerRoot == null) return null;
        var a = _playerRoot.GetComponent<Animator>()
             ?? _playerRoot.GetComponentInChildren<Animator>();
        return a != null ? a.gameObject : _playerRoot;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CLIP DISCOVERY
    // ─────────────────────────────────────────────────────────────────────────

    private void RefreshClips()
    {
        var manual = _clips.Where(c => c != null).ToHashSet();
        _clips.Clear();

        var guids = AssetDatabase.FindAssets(
            "t:AnimationClip",
            new[] { "Assets/Project/Animations/Characters/Player" });

        foreach (var guid in guids)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                AssetDatabase.GUIDToAssetPath(guid));
            if (clip != null) _clips.Add(clip);
        }

        foreach (var c in manual)
            if (!_clips.Contains(c)) _clips.Add(c);

        _clips = _clips.OrderBy(c => c.name).ToList();

        if (_selectedClipIndex >= _clips.Count)
            _selectedClipIndex = _clips.Count > 0 ? 0 : -1;

        Repaint();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UTILITY
    // ─────────────────────────────────────────────────────────────────────────

    private void TryFindPlayer()
    {
        var held = FindFirstObjectByType<PlayerHeldItem>();
        if (held != null)
        {
            _playerRoot = held.gameObject;
            DestroyPreview();
        }
        else
        {
            Debug.LogWarning(
                "[EquipmentPreview] No PlayerHeldItem found in the active scene. " +
                "Open a scene that contains the player, then click Find again.");
        }
        Repaint();
    }

    private static void DrawSectionLabel(string title)
    {
        GUILayout.Space(4);
        var rect = EditorGUILayout.GetControlRect(false, 18);
        EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.16f));
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 10,
            normal   = { textColor = new Color(0.72f, 0.72f, 0.72f) }
        };
        EditorGUI.LabelField(rect, "   " + title, style);
        GUILayout.Space(3);
    }

    private static void DrawDivider()
    {
        GUILayout.Space(5);
        var r = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(r, new Color(0.1f, 0.1f, 0.1f, 0.6f));
        GUILayout.Space(5);
    }
}
