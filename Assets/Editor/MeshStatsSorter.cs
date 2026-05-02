#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MeshStatsSortedWindow : EditorWindow
{
    [MenuItem("Tools/Mesh Stats (Sorted)")]
    public static void Open()
    {
        var window = GetWindow<MeshStatsSortedWindow>("Mesh Stats (Sorted)");
        window.minSize = new Vector2(920, 460);
        window.RefreshImmediate();
    }

    // ---------- Settings ----------
    private bool includeInactive = false;
    private bool includeSkinned = true;

    // View mode for drill-down
    private enum DrillMode { DirectChildren, AllDescendants }
    private DrillMode drillMode = DrillMode.DirectChildren;

    private int topN = 50;
    private bool sortByTris = true;

    // ---------- Results ----------
    private List<Row> rows = new();
    private Vector2 scroll;
    private string search = "";

    // ---------- Drill Stack ----------
    private readonly Stack<GameObject> scopeStack = new();

    // UI state
    private double lastRefreshTime;

    // --- Fix: prevent refresh while drawing ---
    private bool _isDrawing;
    private bool _pendingRefresh;

    private void OnEnable()
    {
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    private void OnDisable()
    {
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
    }

    private void OnHierarchyChanged()
    {
        // อย่า refresh ทันทีระหว่างวาด แค่ repaint ก็พอ
        if (EditorApplication.timeSinceStartup - lastRefreshTime > 0.5)
            Repaint();
    }

    private GameObject CurrentScope => scopeStack.Count > 0 ? scopeStack.Peek() : null;

    private void OnGUI()
    {
        _isDrawing = true;
        try
        {
            DrawToolbar();

            EditorGUILayout.Space(6);

            if (rows == null || rows.Count == 0)
            {
                EditorGUILayout.HelpBox("No results. Click Refresh to scan meshes.", MessageType.Info);
                if (GUILayout.Button("Refresh", GUILayout.Height(28)))
                    RequestRefresh();
                return;
            }

            DrawHeader();

            scroll = EditorGUILayout.BeginScrollView(scroll);

            // ? Fix: ใช้ snapshot กัน list เปลี่ยนระหว่าง enumerate
            var snapshot = rows.ToArray();
            IEnumerable<Row> view = snapshot;

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLowerInvariant();
                view = view.Where(r => r.Path != null && r.Path.ToLowerInvariant().Contains(s));
            }

            int shown = 0;
            foreach (var r in view)
            {
                if (shown >= topN) break;
                DrawRow(r, shown + 1);
                shown++;
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6);
            DrawFooter();
        }
        finally
        {
            _isDrawing = false;

            // ? Fix: ถ้ามีคนกด Drill/Back/Refresh ระหว่างวาด ให้ไปทำหลังจบเฟรม
            if (_pendingRefresh)
            {
                _pendingRefresh = false;
                EditorApplication.delayCall += () =>
                {
                    if (this != null)
                        RefreshImmediate();
                };
            }
        }
    }

    // เรียกแทน Refresh() เพื่อกัน error ระหว่าง OnGUI
    private void RequestRefresh()
    {
        if (_isDrawing)
        {
            _pendingRefresh = true;
            return;
        }

        RefreshImmediate();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginVertical("box");

        // Row 1
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Refresh", GUILayout.Width(90), GUILayout.Height(24)))
            RequestRefresh();

        GUILayout.Space(6);

        using (new EditorGUI.DisabledScope(scopeStack.Count == 0))
        {
            if (GUILayout.Button("Back", GUILayout.Width(70), GUILayout.Height(24)))
            {
                scopeStack.Pop();
                RequestRefresh();
            }
        }

        GUILayout.Space(6);

        if (GUILayout.Button("Select Top 1", GUILayout.Width(110), GUILayout.Height(24)))
        {
            if (rows.Count > 0 && rows[0].Target != null)
            {
                Selection.activeObject = rows[0].Target;
                EditorGUIUtility.PingObject(rows[0].Target);
            }
        }

        GUILayout.FlexibleSpace();

        GUILayout.Label("Search:", GUILayout.Width(50));
        search = EditorGUILayout.TextField(search, GUILayout.Width(240));

        EditorGUILayout.EndHorizontal();

        // Row 2
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();

        includeInactive = EditorGUILayout.ToggleLeft("Include Inactive", includeInactive, GUILayout.Width(130));
        includeSkinned = EditorGUILayout.ToggleLeft("Include Skinned", includeSkinned, GUILayout.Width(140));

        GUILayout.Space(10);

        GUILayout.Label("Drill Mode:", GUILayout.Width(70));
        drillMode = (DrillMode)EditorGUILayout.EnumPopup(drillMode, GUILayout.Width(150));

        GUILayout.FlexibleSpace();

        GUILayout.Label("Show Top:", GUILayout.Width(70));
        topN = EditorGUILayout.IntField(topN, GUILayout.Width(60));
        topN = Mathf.Clamp(topN, 1, 5000);

        GUILayout.Space(8);

        sortByTris = GUILayout.Toggle(sortByTris, "Sort by Tris", "Button", GUILayout.Width(110));
        if (!sortByTris) GUILayout.Toggle(true, "Sort by Verts", "Button", GUILayout.Width(110));

        EditorGUILayout.EndHorizontal();

        // Breadcrumb
        EditorGUILayout.Space(6);
        DrawBreadcrumb();

        EditorGUILayout.EndVertical();
    }

    private void DrawBreadcrumb()
    {
        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("Scope:", GUILayout.Width(45));

        var scene = SceneManager.GetActiveScene();
        GUILayout.Label(scene.IsValid() ? scene.name : "(No Scene)", GUILayout.Width(200));

        if (scopeStack.Count > 0)
        {
            GUILayout.Label(" > ", GUILayout.Width(18));

            // ใช้ Reverse() ทำ breadcrumb เฉย ๆ (ไม่แก้ list หลัก)
            var arr = scopeStack.Reverse().ToArray();
            string path = string.Join(" > ", arr.Select(a => a != null ? a.name : "null"));
            GUILayout.Label(path);
        }
        else
        {
            GUILayout.Label(" (Root Overview)");
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("#", GUILayout.Width(26));
        GUILayout.Label("GameObject", GUILayout.Width(360));
        GUILayout.Label("Meshes", GUILayout.Width(60));
        GUILayout.Label("SubMeshes", GUILayout.Width(80));
        GUILayout.Label("Tris", GUILayout.Width(90));
        GUILayout.Label("Verts", GUILayout.Width(90));
        GUILayout.FlexibleSpace();
        GUILayout.Label("Actions", GUILayout.Width(180));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    }

    private void DrawRow(Row r, int rank)
    {
        EditorGUILayout.BeginHorizontal();

        GUILayout.Label(rank.ToString("00"), GUILayout.Width(26));

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField(r.Target, typeof(GameObject), true, GUILayout.Width(240));
        }

        if (GUILayout.Button(new GUIContent(r.Path, "Click to copy full path"), GUILayout.Width(120)))
            EditorGUIUtility.systemCopyBuffer = r.Path ?? "";

        GUILayout.Label(r.MeshCount.ToString(), GUILayout.Width(60));
        GUILayout.Label(r.SubMeshCount.ToString(), GUILayout.Width(80));
        GUILayout.Label(r.Tris.ToString("n0"), GUILayout.Width(90));
        GUILayout.Label(r.Verts.ToString("n0"), GUILayout.Width(90));

        GUILayout.FlexibleSpace();

        using (new EditorGUI.DisabledScope(!CanDrill(r.Target)))
        {
            if (GUILayout.Button("Drill", GUILayout.Width(55)))
            {
                scopeStack.Push(r.Target);
                RequestRefresh();
            }
        }

        if (GUILayout.Button("Select", GUILayout.Width(55)))
        {
            if (r.Target != null)
            {
                Selection.activeObject = r.Target;
                EditorGUIUtility.PingObject(r.Target);
            }
        }

        if (GUILayout.Button("Ping", GUILayout.Width(55)))
        {
            if (r.Target != null)
                EditorGUIUtility.PingObject(r.Target);
        }

        EditorGUILayout.EndHorizontal();
    }

    private bool CanDrill(GameObject go)
    {
        if (go == null) return false;
        if (go.transform.childCount == 0 && drillMode == DrillMode.DirectChildren) return false;

        if (drillMode == DrillMode.AllDescendants)
        {
            var mf = go.GetComponentsInChildren<MeshFilter>(includeInactive);
            var sk = includeSkinned
                ? go.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive)
                : Array.Empty<SkinnedMeshRenderer>();

            return (mf != null && mf.Length > 0) || (sk != null && sk.Length > 0);
        }

        return go.transform.childCount > 0;
    }

    private void DrawFooter()
    {
        long totalTris = rows.Sum(r => (long)r.Tris);
        long totalVerts = rows.Sum(r => (long)r.Verts);

        EditorGUILayout.BeginHorizontal("box");
        GUILayout.Label($"Entries: {rows.Count:n0}", GUILayout.Width(140));
        GUILayout.Label($"Total Tris: {totalTris:n0}", GUILayout.Width(200));
        GUILayout.Label($"Total Verts: {totalVerts:n0}", GUILayout.Width(200));
        GUILayout.FlexibleSpace();
        GUILayout.Label("Tip: Click Path to copy", GUILayout.Width(180));
        EditorGUILayout.EndHorizontal();
    }

    // --- Main refresh logic (เดิม) ---
    private void RefreshImmediate()
    {
        lastRefreshTime = EditorApplication.timeSinceStartup;

        rows.Clear();

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
            return;

        var scope = CurrentScope;

        if (scope == null)
        {
            foreach (var root in activeScene.GetRootGameObjects())
            {
                if (!includeInactive && !root.activeInHierarchy) continue;

                var stats = CalcStats(root, includeDescendants: true);
                if (stats.Tris > 0 || stats.Verts > 0)
                    rows.Add(stats);
            }
        }
        else
        {
            if (drillMode == DrillMode.DirectChildren)
            {
                foreach (Transform child in scope.transform)
                {
                    var go = child.gameObject;
                    if (!includeInactive && !go.activeInHierarchy) continue;

                    var stats = CalcStats(go, includeDescendants: true);
                    if (stats.Tris > 0 || stats.Verts > 0)
                        rows.Add(stats);
                }
            }
            else // AllDescendants
            {
                var all = scope.GetComponentsInChildren<Transform>(includeInactive)
                               .Select(t => t.gameObject)
                               .Where(go => go != scope);

                foreach (var go in all)
                {
                    if (go == null) continue;
                    if (!includeInactive && !go.activeInHierarchy) continue;

                    var stats = CalcStats(go, includeDescendants: false);
                    if (stats.Tris > 0 || stats.Verts > 0)
                        rows.Add(stats);
                }
            }
        }

        rows = sortByTris
            ? rows.OrderByDescending(r => r.Tris).ThenByDescending(r => r.Verts).ToList()
            : rows.OrderByDescending(r => r.Verts).ThenByDescending(r => r.Tris).ToList();

        Repaint();
    }

    private Row CalcStats(GameObject root, bool includeDescendants)
    {
        int tris = 0;
        int verts = 0;
        int meshCount = 0;
        int subMeshCount = 0;

        if (includeDescendants)
        {
            var mfs = root.GetComponentsInChildren<MeshFilter>(includeInactive);
            foreach (var mf in mfs)
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;
                meshCount++;
                verts += mesh.vertexCount;
                tris += mesh.triangles.Length / 3;
                subMeshCount += mesh.subMeshCount;
            }

            if (includeSkinned)
            {
                var sks = root.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive);
                foreach (var sk in sks)
                {
                    var mesh = sk.sharedMesh;
                    if (mesh == null) continue;
                    meshCount++;
                    verts += mesh.vertexCount;
                    tris += mesh.triangles.Length / 3;
                    subMeshCount += mesh.subMeshCount;
                }
            }
        }
        else
        {
            var mf = root.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh)
            {
                var mesh = mf.sharedMesh;
                meshCount++;
                verts += mesh.vertexCount;
                tris += mesh.triangles.Length / 3;
                subMeshCount += mesh.subMeshCount;
            }

            if (includeSkinned)
            {
                var sk = root.GetComponent<SkinnedMeshRenderer>();
                if (sk && sk.sharedMesh)
                {
                    var mesh = sk.sharedMesh;
                    meshCount++;
                    verts += mesh.vertexCount;
                    tris += mesh.triangles.Length / 3;
                    subMeshCount += mesh.subMeshCount;
                }
            }
        }

        return new Row
        {
            Target = root,
            Path = GetPath(root.transform),
            Tris = tris,
            Verts = verts,
            MeshCount = meshCount,
            SubMeshCount = subMeshCount
        };
    }

    private static string GetPath(Transform t)
    {
        var stack = new Stack<string>();
        while (t != null)
        {
            stack.Push(t.name);
            t = t.parent;
        }
        return string.Join("/", stack);
    }

    [Serializable]
    private class Row
    {
        public GameObject Target;
        public string Path;
        public int Tris;
        public int Verts;
        public int MeshCount;
        public int SubMeshCount;
    }
}
#endif
