using UnityEngine;

public class VFXSnapToGround : MonoBehaviour
{
    void Start()
    {
        // ยิงหาพื้นทันทีที่เกิด
        RaycastHit hit;
        Vector3 startPos = new Vector3(transform.position.x, 500f, transform.position.z);

        if (Physics.Raycast(startPos, Vector3.down, out hit, 1000f, ~0, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point + Vector3.up * 0.1f;
        }
    }
}