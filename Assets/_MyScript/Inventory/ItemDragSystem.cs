using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton ที่จัดการ Drag Icon ที่ลอยตามเมาส์
///
/// Setup ใน Canvas (ชั้นบนสุด — เหนือทุก UI อื่น):
///   DragIconRoot  (GameObject ว่าง — ติด Script นี้)
///     └── DragIcon  (Image, RaycastTarget=OFF, 60x60)
///           └── DragAmount  (TMP, มุมล่างขวา)
///
/// แล้วลาก DragIcon Image และ DragAmount TMP ใส่ Inspector
/// </summary>
public class ItemDragSystem : MonoBehaviour
{
    public static ItemDragSystem Instance { get; private set; }

    [Header("Drag Icon (ลอยตามเมาส์)")]
    public RectTransform dragIconRect;
    public Image         dragIconImage;
    public TextMeshProUGUI dragAmountText;

    // ─── State ────────────────────────────────────────────────────────
    public static bool   IsDragging   { get; private set; }
    public static ItemSO CurrentItem  { get; private set; }
    public static int    CurrentAmount{ get; private set; }

    static Action _onConsumed;   // เรียกเมื่อ drop สำเร็จ

    Canvas _canvas;

    // ──────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        _canvas = GetComponentInParent<Canvas>();
        if (dragIconRect) dragIconRect.gameObject.SetActive(false);
    }

    // ─── API ──────────────────────────────────────────────────────────

    public static void BeginDrag(ItemSO item, int amount, Action onConsumed)
    {
        if (Instance == null || item == null) return;

        CurrentItem   = item;
        CurrentAmount = amount;
        _onConsumed   = onConsumed;
        IsDragging    = true;

        if (Instance.dragIconImage)
            Instance.dragIconImage.sprite = item.icon;

        if (Instance.dragAmountText)
            Instance.dragAmountText.text = amount > 1 ? amount.ToString() : "";

        if (Instance.dragIconRect)
            Instance.dragIconRect.gameObject.SetActive(true);
    }

    public static void UpdatePosition(Vector2 screenPos)
    {
        if (!IsDragging || Instance == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            Instance._canvas.transform as RectTransform,
            screenPos,
            Instance._canvas.worldCamera,
            out Vector2 local);

        Instance.dragIconRect.localPosition = local;
    }

    /// <summary>TrashDropZone เรียกเมื่อรับของสำเร็จ</summary>
    public static void Consume()
    {
        IsDragging = false;
        _onConsumed?.Invoke();
        _onConsumed = null;
        CurrentItem  = null;
        CurrentAmount = 0;
        if (Instance?.dragIconRect) Instance.dragIconRect.gameObject.SetActive(false);
    }

    /// <summary>เรียกเมื่อ drag ยกเลิก (drop ผิดที่)</summary>
    public static void CancelDrag()
    {
        IsDragging    = false;
        _onConsumed   = null;
        CurrentItem   = null;
        CurrentAmount = 0;
        if (Instance?.dragIconRect) Instance.dragIconRect.gameObject.SetActive(false);
    }
}
