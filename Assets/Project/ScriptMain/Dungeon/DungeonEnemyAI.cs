// DungeonEnemyAI.cs
// Enemy AI พื้นฐาน — ไล่ตาม Player, โจมตี, รับ Damage
// ต้องการ NavMeshAgent บน GameObject (bake NavMesh ใน Dungeon Scene ก่อน)

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class DungeonEnemyAI : MonoBehaviour
{
    [Header("Stats (ถูก override โดย DungeonEnemySO.GetHealthForFloor)")]
    public int   maxHP         = 20;
    public int   attackDamage  = 5;

    [Header("Behavior")]
    public float detectionRadius = 8f;   // ระยะเริ่มไล่
    public float attackRadius    = 1.5f; // ระยะโจมตี
    public float attackCooldown  = 1.5f; // วิต่อครั้ง

    [Header("Animator Parameters (optional)")]
    public string walkAnimBool    = "IsRunning";
    public string attackAnimTrig  = "Chop";    // ใช้ Chop ที่มีอยู่แล้ว
    public string dieAnimTrig     = "Die";

    // ──────────────────────────────────────────────────────────────────────
    // Runtime
    // ──────────────────────────────────────────────────────────────────────
    private int           currentHP;
    private bool          isDead = false;
    private float         lastAttackTime;

    private Transform     player;
    private NavMeshAgent  agent;
    private Animator      animator;

    // ──────────────────────────────────────────────────────────────────────
    /// <summary>เรียกจาก DungeonManager หลัง NavMesh bake เสร็จ</summary>
    public void Init(MyGame.Dungeon.DungeonEnemySO data, int floor)
    {
        if (data != null)
        {
            maxHP        = data.GetHealthForFloor(floor);
            attackDamage = data.GetDamageForFloor(floor);
        }
        currentHP = maxHP;
        agent     = GetComponent<NavMeshAgent>();
        animator  = GetComponent<Animator>();

        // ใช้ LocalPlayerUtil แทน FindGameObjectWithTag ตรงๆ — สำคัญมากเพราะ dungeon เป็น per-player
        // instance ศัตรูของ instance ผู้เล่นคนไหนต้องไล่ตามผู้เล่นคนนั้นเท่านั้น ถ้าจับผิดตัวจะไล่ตาม
        // ผู้เล่นอีกคนข้าม instance ไปเลย
        player = LocalPlayerUtil.GetLocalPlayerTransform();

        enabled = true; // เปิด Update
    }

    void Start()
    {
        // Init() จะถูกเรียกจาก DungeonManager แทน
        // ถ้าไม่มีการเรียก Init ให้ init ด้วยค่า default
        if (player == null)
        {
            currentHP = maxHP;
            agent     = GetComponent<NavMeshAgent>();
            animator  = GetComponent<Animator>();
            player    = LocalPlayerUtil.GetLocalPlayerTransform();
        }
    }

    void Update()
    {
        if (isDead || !player) return;

        float dist = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(player.position.x,    player.position.z));

        if (dist <= attackRadius)
        {
            // หยุดเดิน — โจมตี
            if (agent) agent.SetDestination(transform.position);
            SetWalking(false);

            if (Time.time >= lastAttackTime + attackCooldown)
                Attack();
        }
        else if (dist <= detectionRadius)
        {
            // วิ่งตาม Player
            if (agent) agent.SetDestination(player.position);
            SetWalking(true);
        }
        else
        {
            // Idle
            if (agent) agent.SetDestination(transform.position);
            SetWalking(false);
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // Combat
    // ──────────────────────────────────────────────────────────────────────
    void Attack()
    {
        lastAttackTime = Time.time;

        // หันหน้าไปหา Player
        Vector3 dir = (player.position - transform.position); dir.y = 0;
        if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);

        // เล่น animation โจมตี
        TriggerAnim(attackAnimTrig);

        // ดีเลย์ damage ให้ตรงกับ animation
        StartCoroutine(DealDamageDelayed(0.4f));
    }

    IEnumerator DealDamageDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!isDead && PlayerHealth.Instance != null)
            PlayerHealth.Instance.TakeDamage(attackDamage);
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;
        currentHP -= dmg;
        if (currentHP <= 0) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (agent) agent.enabled = false;
        TriggerAnim(dieAnimTrig);

        // Disable collider
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        Destroy(gameObject, 2f);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Animator Helpers
    // ──────────────────────────────────────────────────────────────────────
    void SetWalking(bool walking)
    {
        if (!animator || string.IsNullOrEmpty(walkAnimBool)) return;
        animator.SetBool(walkAnimBool, walking);
    }

    void TriggerAnim(string trigName)
    {
        if (!animator || string.IsNullOrEmpty(trigName)) return;
        foreach (var p in animator.parameters)
            if (p.name == trigName && p.type == AnimatorControllerParameterType.Trigger)
            { animator.SetTrigger(trigName); return; }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
        Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
