using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPCListRowUI : MonoBehaviour
{
    [SerializeField] private Image            portraitImage;
    [SerializeField] private TextMeshProUGUI  nameText;
    [SerializeField] private HeartMeterUI     heartMeter;
    [SerializeField] private Image            selectedHighlight;
    [SerializeField] private Button           button;

    [Tooltip("overlay ที่แสดงเมื่อ NPC ยังไม่ได้พบ เช่น ไอคอน lock หรือ dim layer (optional)")]
    [SerializeField] private GameObject unmetOverlay;

    [Tooltip("สี Portrait เมื่อยังไม่ได้พบ (ปกติจะ dim เทา)")]
    [SerializeField] private Color unmetPortraitColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    public int NpcId { get; private set; }
    public event Action<int> OnClicked;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(() => OnClicked?.Invoke(NpcId));
    }

    /// <param name="def">ข้อมูล NPC</param>
    /// <param name="heartLevel">ระดับหัวใจปัจจุบัน (0 ถ้ายังไม่พบ)</param>
    /// <param name="hasMet">true = เคยพบแล้ว, false = ยังไม่เคยพบ</param>
    public void Setup(NPCDefinitionSO def, int heartLevel, bool hasMet)
    {
        NpcId = def.npcId;

        if (portraitImage != null)
        {
            portraitImage.sprite = def.portrait;
            portraitImage.color  = hasMet ? Color.white : unmetPortraitColor;
        }

        if (nameText != null) nameText.text = def.displayName;

        heartMeter?.SetHearts(def.maxHeartLevel, hasMet ? heartLevel : 0);

        if (unmetOverlay != null) unmetOverlay.SetActive(!hasMet);

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null) selectedHighlight.enabled = selected;
    }
}
