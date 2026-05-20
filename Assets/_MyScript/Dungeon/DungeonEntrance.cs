// DungeonEntrance.cs
// วางไว้ที่ปากถ้ำ/ทางเข้า Dungeon — กด E เพื่อเข้า

using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonEntrance : MonoBehaviour
{
    [Header("Interaction")]
    public float   interactRadius  = 2f;
    public KeyCode interactKey     = KeyCode.E;
    public GameObject promptUI;          // ป้าย "[E] เข้าถ้ำ"

    [Header("Scene")]
    public string dungeonSceneName = "Dungeon";  // ชื่อ Scene ถ้ำ

    private Transform player;

    // ──────────────────────────────────────────────────────────────────────
    void Start()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go) player = go.transform;
    }

    void Update()
    {
        if (!player) return;

        float dist   = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(player.position.x,    player.position.z));
        bool nearby  = dist <= interactRadius;

        if (promptUI) promptUI.SetActive(nearby);

        if (nearby && Input.GetKeyDown(interactKey))
            EnterDungeon();
    }

    // ──────────────────────────────────────────────────────────────────────
    void EnterDungeon()
    {
        // บันทึก Scene + position ปัจจุบัน เพื่อ return กลับมา
        DungeonReturnData.farmScene       = SceneManager.GetActiveScene().name;
        DungeonReturnData.returnPosition  = player.position;
        DungeonReturnData.returnFromDeath = false;

        SceneManager.LoadScene(dungeonSceneName);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
