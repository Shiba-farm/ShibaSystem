using UnityEngine;

public class TileCursor : MonoBehaviour
{
    // Singleton — ให้ Script อื่นเข้าถึงตำแหน่ง Cursor ได้
    public static TileCursor Instance { get; private set; }

    /// <summary>ตำแหน่งโลกที่ Cursor ชี้อยู่ (หลัง Grid Snap)</summary>
    public Vector3 WorldPosition => cursorVisual ? cursorVisual.transform.position : Vector3.zero;

    /// <summary>Cursor กำลังแสดงอยู่ไหม (มีเป้าหมายในรัศมี)</summary>
    public bool IsActive => cursorVisual != null && cursorVisual.activeSelf;

    [Header("References")]
    public Camera cam;
    public Transform player;
    public GameObject cursorVisual;

    [Header("Layer Settings")]
    public LayerMask soilMask;
    public LayerMask treeMask;
    [Tooltip("��� Layer �ͧ��鹴Թ���� (�� Default ���� Terrain)")]
    public LayerMask groundMask; // [��������] ��������鹴Թ����

    [Header("Cursor Settings")]
    public float interactRange = 4f;
    public Vector3 visualOffset = new Vector3(0, 0.05f, 0);

    [Header("Grid Snapping (Ẻ��� 2)")]
    public bool snapToGrid = true;
    [Tooltip("��Ҵ�ͧ��ͧ��Դ (�����ŧ�Թ��Ҩ� 1x1 ����)")]
    public float gridSize = 1f;

    [Header("Debug")]
    public bool showDebugRay = true;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (!cam) cam = Camera.main;
        if (!player) { var p = GameObject.FindGameObjectWithTag("Player"); if (p) player = p.transform; }
        if (cursorVisual) cursorVisual.SetActive(false);
    }

    private void Update()
    {
        if (InventoryMainUI.IsOpen) { if (cursorVisual) cursorVisual.SetActive(false); return; }
        UpdateCursorPosition();
    }

    void UpdateCursorPosition()
    {
        if (!cursorVisual || !player || !cam) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        bool foundTarget = false;
        Vector3 targetPos = Vector3.zero;

        if (showDebugRay) Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red);

        // 1. ��������·���� "�����" ��͹
        if (Physics.Raycast(ray, out hit, 100f, treeMask))
        {
            if (Vector3.Distance(FlatPos(player.position), FlatPos(hit.point)) <= interactRange)
            {
                foundTarget = true;
                targetPos = hit.transform.position + visualOffset;
            }
        }

        // 2. �������͵���� �����������·���� "�ŧ�Թ (SoilTile)" ����Ѻ�ͺ�������
        if (!foundTarget && Physics.Raycast(ray, out hit, 100f, soilMask))
        {
            SoilTile tile = hit.collider.GetComponentInParent<SoilTile>();
            if (tile != null)
            {
                if (Vector3.Distance(FlatPos(player.position), FlatPos(hit.point)) <= interactRange)
                {
                    foundTarget = true;
                    targetPos = tile.transform.position + visualOffset;
                }
            }
        }

        // 3. [��������ش!] ��������������� ����� "��鹴Թ����" ��������ͺ������Ѻ�ͺ
        if (!foundTarget && Physics.Raycast(ray, out hit, 100f, groundMask))
        {
            if (Vector3.Distance(FlatPos(player.position), FlatPos(hit.point)) <= interactRange)
            {
                foundTarget = true;

                // ��ͤ�ԡѴ����繪�ͧ���ҧ (Grid Snapping)
                if (snapToGrid)
                {
                    float snapX = Mathf.Round(hit.point.x / gridSize) * gridSize;
                    float snapZ = Mathf.Round(hit.point.z / gridSize) * gridSize;
                    // �������٧ (Y) Ṻ仡Ѻ��鹼�Ƿ���������ԧ��
                    targetPos = new Vector3(snapX, hit.point.y, snapZ) + visualOffset;
                }
                else
                {
                    targetPos = hit.point + visualOffset;
                }
            }
        }

        // 4. �ʴ��š�ͺ������
        if (foundTarget)
        {
            cursorVisual.SetActive(true);
            cursorVisual.transform.position = Vector3.Lerp(cursorVisual.transform.position, targetPos, 25f * Time.deltaTime);
        }
        else
        {
            cursorVisual.SetActive(false);
        }
    }

    private Vector3 FlatPos(Vector3 pos)
    {
        return new Vector3(pos.x, 0, pos.z);
    }
}