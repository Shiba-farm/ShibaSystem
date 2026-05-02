using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// ระบบตกปลาหลัก — จัดการ flow ทั้งหมด:
/// 1. Player เข้า FishingZone → แสดง prompt "กด F"
/// 2. กด F → โยนเบ็ด (animation Fish) → รอปลากัด
/// 3. ปลากัด → แสดง "!" + เปิด MiniGame
/// 4. MiniGame เสร็จ → ได้ปลา / miss
///
/// วาง script นี้บน GameManagerSystem หรือ GameObject ใหม่ชื่อ "FishingSystem"
/// </summary>
public class FishingSystem : MonoBehaviour
{
    public static FishingSystem Instance { get; private set; }

    // ─── Config ───────────────────────────────────────────────────────
    [Header("Keys")]
    public KeyCode fishKey = KeyCode.F;

    [Header("Timing")]
    [Tooltip("เวลาต่ำสุดก่อนปลากัด (วินาที)")]
    public float minWaitTime = 2f;
    [Tooltip("เวลาสูงสุดก่อนปลากัด (วินาที)")]
    public float maxWaitTime = 7f;
    [Tooltip("ความยากของ mini-game (1 = ปกติ, 2 = ยากขึ้น)")]
    public float fishingDifficulty = 1f;

    [Header("UI")]
    [Tooltip("แผง prompt 'กด F เพื่อตกปลา'")]
    public GameObject fishingPromptUI;
    public TextMeshProUGUI promptText;

    [Tooltip("แผง '! ปลากัด!' (เตือนให้กด Space)")]
    public GameObject biteIndicatorUI;
    public TextMeshProUGUI biteText;

    [Header("Result Text (optional)")]
    public TextMeshProUGUI catchResultText;
    public float resultDisplayTime = 2f;

    [Header("SFX (optional)")]
    public AudioSource audioSource;
    public AudioClip castSound;
    public AudioClip biteSound;
    public AudioClip catchSound;
    public AudioClip missSound;

    // ─── Runtime ──────────────────────────────────────────────────────
    FishingZone _currentZone;
    bool _isFishing;

    PlayerController _player;
    Animator _playerAnimator;

    Coroutine _waitCoroutine;
    Coroutine _resultCoroutine;

    // ──────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        _player = FindObjectOfType<PlayerController>();
        if (_player) _playerAnimator = _player.GetComponent<Animator>();

        // ซ่อน UI เริ่มต้น
        if (fishingPromptUI) fishingPromptUI.SetActive(false);
        if (biteIndicatorUI) biteIndicatorUI.SetActive(false);
        if (catchResultText) catchResultText.text = "";

        // subscribe mini-game event
        if (FishingMiniGameUI.Instance)
            FishingMiniGameUI.Instance.OnResult += OnMiniGameResult;
    }

    void OnDestroy()
    {
        if (FishingMiniGameUI.Instance)
            FishingMiniGameUI.Instance.OnResult -= OnMiniGameResult;
    }

    void Update()
    {
        // ไม่รับ input ถ้าไม่มี zone / กำลังตกอยู่ / เปิด inventory
        if (_currentZone == null || _isFishing) return;
        if (InventoryMainUI.IsOpen) return;

        if (Input.GetKeyDown(fishKey))
            BeginFishing();
    }

    // ─── Zone ─────────────────────────────────────────────────────────

    public void EnterZone(FishingZone zone)
    {
        _currentZone = zone;
        ShowPrompt($"กด [{fishKey}] เพื่อตกปลา\n<size=80%>{zone.zoneData?.zoneName}</size>");
    }

    public void ExitZone(FishingZone zone)
    {
        if (_currentZone != zone) return;
        _currentZone = null;
        HidePrompt();
        if (_isFishing) ForceStopFishing();
    }

    // ─── Flow ─────────────────────────────────────────────────────────

    void BeginFishing()
    {
        _isFishing = true;
        HidePrompt();

        // หันหน้าไปทิศน้ำ
        if (_currentZone?.fishingDirectionPoint != null && _player)
        {
            Vector3 dir = _currentZone.fishingDirectionPoint.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                _player.transform.rotation = Quaternion.LookRotation(dir);
        }

        // Lock player movement (ไม่ lock ถ้าอยู่บนเรือ — BoatController จัดการอยู่แล้ว)
        if (_player && BoatController.ActiveBoat == null)
            _player.SetBusy(true);

        // Animation: โยนเบ็ด
        if (_playerAnimator)
        {
            _playerAnimator.SetTrigger("Fish");
            _playerAnimator.SetBool("IsFishing", true);
        }

        PlaySound(castSound);

        // รอปลากัด
        if (_waitCoroutine != null) StopCoroutine(_waitCoroutine);
        _waitCoroutine = StartCoroutine(WaitForBite());
    }

    IEnumerator WaitForBite()
    {
        float waitTime = Random.Range(minWaitTime, maxWaitTime);
        yield return new WaitForSeconds(waitTime);

        if (!_isFishing) yield break;

        // ปลากัด!
        PlaySound(biteSound);
        ShowBiteIndicator("! ปลากัด! กด Space!");

        if (FishingMiniGameUI.Instance)
            FishingMiniGameUI.Instance.Show(fishingDifficulty);
    }

    void OnMiniGameResult(FishingMiniGameUI.CatchResult result)
    {
        HideBiteIndicator();

        if (result == FishingMiniGameUI.CatchResult.Miss)
        {
            PlaySound(missSound);
            ShowCatchResult("💨 ปลาหนีไปแล้ว!", Color.gray);
            StopFishing();
            return;
        }

        // ─── จับปลาได้! ───────────────────────────────────────────────
        PlaySound(catchSound);

        var entry = _currentZone?.zoneData?.RollFish();
        if (entry != null && entry.fish != null)
        {
            int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);

            // ใส่ Hotbar ก่อน → ถ้าไม่มีที่ว่างไปใส่ Inventory
            bool added = false;
            if (HotbarUI.Instance != null)
                added = HotbarUI.Instance.AddItemToFirstEmptySlot(entry.fish, amount);
            if (!added && InventoryMainUI.Instance != null)
                InventoryMainUI.Instance.AddItemToInventory(entry.fish, amount);

            string bonus = result == FishingMiniGameUI.CatchResult.Perfect ? " ✨ Perfect!" : "";
            ShowCatchResult($"ได้ {entry.fish.itemName} x{amount}{bonus}", Color.green);
            Debug.Log($"[Fishing] ได้ {entry.fish.itemName} x{amount}{bonus}");
        }
        else
        {
            ShowCatchResult("ตกปลาสำเร็จแต่ไม่มีปลาในโซนนี้", Color.yellow);
        }

        StopFishing();
    }

    void StopFishing()
    {
        _isFishing = false;

        if (_waitCoroutine != null) { StopCoroutine(_waitCoroutine); _waitCoroutine = null; }

        // คืน animation
        if (_playerAnimator)
            _playerAnimator.SetBool("IsFishing", false);

        // Unlock player เฉพาะถ้าไม่ได้อยู่บนเรือ
        if (_player && BoatController.ActiveBoat == null)
            _player.SetBusy(false);

        HideBiteIndicator();

        // แสดง prompt ใหม่ถ้ายังอยู่ใน zone
        if (_currentZone != null)
            ShowPrompt($"กด [{fishKey}] เพื่อตกปลา\n<size=80%>{_currentZone.zoneData?.zoneName}</size>");
    }

    /// <summary>หยุดตกปลาทันทีโดยไม่ให้ผล (เรียกตอน ExitZone)</summary>
    void ForceStopFishing()
    {
        _isFishing = false;
        if (_waitCoroutine != null) { StopCoroutine(_waitCoroutine); _waitCoroutine = null; }
        if (_playerAnimator) _playerAnimator.SetBool("IsFishing", false);
        if (_player && BoatController.ActiveBoat == null) _player.SetBusy(false);
        FishingMiniGameUI.Instance?.Hide();
        HideBiteIndicator();
    }

    // ─── UI Helpers ───────────────────────────────────────────────────

    void ShowPrompt(string msg)
    {
        if (fishingPromptUI) fishingPromptUI.SetActive(true);
        if (promptText) promptText.text = msg;
    }

    void HidePrompt()
    {
        if (fishingPromptUI) fishingPromptUI.SetActive(false);
    }

    void ShowBiteIndicator(string msg)
    {
        if (biteIndicatorUI) biteIndicatorUI.SetActive(true);
        if (biteText) biteText.text = msg;
    }

    void HideBiteIndicator()
    {
        if (biteIndicatorUI) biteIndicatorUI.SetActive(false);
    }

    void ShowCatchResult(string msg, Color color)
    {
        if (catchResultText == null) return;
        catchResultText.text  = msg;
        catchResultText.color = color;
        if (_resultCoroutine != null) StopCoroutine(_resultCoroutine);
        _resultCoroutine = StartCoroutine(ClearResultAfter(resultDisplayTime));
    }

    IEnumerator ClearResultAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (catchResultText) catchResultText.text = "";
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource && clip) audioSource.PlayOneShot(clip);
    }
}
