using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// F1 = raycast hits at mouse (full path)
/// F2 = CanvasGroups that block/disable input
/// F3 = deep inspect top hit (CanvasGroup chain + DraggableItem info)
/// F4 = DRAG ZONE DIAGNOSTIC — full path + ScrollRect ancestor check
/// </summary>
public class RaycastDebugger : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            var results = new List<RaycastResult>();
            var eventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            EventSystem.current.RaycastAll(eventData, results);

            Debug.Log($"[RaycastDebugger] ====== F1 Raycast at {Input.mousePosition} — {results.Count} hits ======");
            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i]; var go = r.gameObject;
                string flags = "";
                if (go.GetComponent<DraggableItem>()    != null) flags += " [DRAG]";
                if (go.GetComponent<InventoryItems>()   != null) flags += " [InvItem]";
                if (go.GetComponent<InventorySlotUIs>() != null) flags += " [Slot]";
                var cg  = go.GetComponent<CanvasGroup>();
                if (cg  != null) flags += $" [CG blk={cg.blocksRaycasts} int={cg.interactable}]";
                var img = go.GetComponent<Image>();
                if (img != null) flags += $" [Img rt={img.raycastTarget}]";
                // full path — no truncation
                Debug.Log($"  [{i}] d={r.depth} | {FullPath(go.transform)}{flags}");
            }
            Debug.Log("[RaycastDebugger] =====================================");
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("[RaycastDebugger] ====== F2 All CanvasGroups ======");
            foreach (var cg in FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None))
                if (!cg.blocksRaycasts || !cg.interactable)
                    Debug.Log($"  BLOCKING: {ShortPath(cg.transform, 5)} blks={cg.blocksRaycasts} int={cg.interactable}");
            Debug.Log("[RaycastDebugger] ===================================");
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            var results = new List<RaycastResult>();
            var eventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            EventSystem.current.RaycastAll(eventData, results);
            if (results.Count == 0) { Debug.Log("[RaycastDebugger] F3: no hits"); return; }

            RaycastResult top = results[0];
            int maxDepth = int.MinValue; bool foundDrag = false;
            foreach (var r in results)
            {
                if (r.gameObject.GetComponent<DraggableItem>() != null && !foundDrag) { top = r; foundDrag = true; }
                if (!foundDrag && r.depth > maxDepth) { maxDepth = r.depth; top = r; }
            }

            Debug.Log($"[RaycastDebugger] ====== F3 Deep Inspect: {top.gameObject.name} (d={top.depth}) {(foundDrag ? "[found DRAG]" : "[max-depth]")} ======");
            Transform t = top.gameObject.transform;
            while (t != null)
            {
                var cg = t.GetComponent<CanvasGroup>();
                if (cg != null) Debug.Log($"  CG @ {ShortPath(t, 5)}: blks={cg.blocksRaycasts} int={cg.interactable} alpha={cg.alpha:F2}");
                var drag = t.GetComponent<DraggableItem>();
                if (drag != null) {
                    var ii = t.GetComponent<InventoryItems>();
                    Debug.Log($"  DRAGGABLE @ {ShortPath(t, 5)} item={ii?.item?.itemName ?? "null"} srcSlot={ii?.sourceSlot?.slotIndex.ToString() ?? "null"}");
                }
                t = t.parent;
            }
            Debug.Log("[RaycastDebugger] =====================================");
        }

        // F4: DRAG ZONE DIAGNOSTIC
        // ► วาง mouse ไว้บนไอเทมที่ลากไม่ได้ → กด F4
        // ► วาง mouse ไว้บนไอเทมที่ลากได้ → กด F4
        // เปรียบเทียบผลทั้งสอง เพื่อหาว่ามีอะไรต่างกัน
        if (Input.GetKeyDown(KeyCode.F4))
        {
            var results = new List<RaycastResult>();
            var eventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            EventSystem.current.RaycastAll(eventData, results);

            Debug.Log($"[RaycastDebugger] ====== F4 DRAG DIAGNOSTIC at {Input.mousePosition} — {results.Count} hits ======");

            // 1) หา DraggableItem ที่ depth สูงสุด (ถ้ามี)
            DraggableItem topDrag = null; int topDragDepth = int.MinValue;
            foreach (var r in results)
            {
                var d = r.gameObject.GetComponent<DraggableItem>();
                if (d != null && r.depth > topDragDepth) { topDrag = d; topDragDepth = r.depth; }
            }

            if (topDrag != null)
                Debug.Log($"  ✓ DRAG FOUND: {FullPath(topDrag.transform)} depth={topDragDepth}");
            else
                Debug.Log("  ✗ NO DraggableItem found at this position!");

            // 2) หา element ที่ depth สูงสุดในทุก result
            RaycastResult maxDepthResult = default; int maxD = int.MinValue;
            foreach (var r in results) if (r.depth > maxD) { maxD = r.depth; maxDepthResult = r; }

            if (maxDepthResult.gameObject != null)
            {
                bool isTheDrag = maxDepthResult.gameObject.GetComponent<DraggableItem>() != null;
                Debug.Log($"  TOP element: {FullPath(maxDepthResult.gameObject.transform)} depth={maxD}{(isTheDrag ? " [same as DRAG ✓]" : " [DIFFERENT from DRAG — this is the BLOCKER!]")}");
            }

            // 3) ตรวจ ScrollRect ใน ancestors ของ DraggableItem
            if (topDrag != null)
            {
                Debug.Log("  — Checking ancestors of DraggableItem for ScrollRect —");
                Transform p = topDrag.transform.parent;
                bool foundScroll = false;
                while (p != null)
                {
                    var sr = p.GetComponent<ScrollRect>();
                    if (sr != null)
                    {
                        Debug.Log($"  ⚠ SCROLL RECT FOUND in ancestor: {FullPath(p)} H={sr.horizontal} V={sr.vertical}");
                        foundScroll = true;
                    }
                    p = p.parent;
                }
                if (!foundScroll) Debug.Log("  No ScrollRect in ancestor chain ✓");
            }

            // 4) log ทุก hit พร้อม full path
            Debug.Log("  — All hits (full paths) —");
            foreach (var r in results)
            {
                var go = r.gameObject;
                string tags = "";
                if (go.GetComponent<DraggableItem>()    != null) tags += "[DRAG]";
                if (go.GetComponent<InventoryItems>()   != null) tags += "[Inv]";
                if (go.GetComponent<InventorySlotUIs>() != null) tags += "[Slot]";
                if (go.GetComponent<ScrollRect>()       != null) tags += "[SCROLL!]";
                var cg = go.GetComponent<CanvasGroup>();
                if (cg != null) tags += $"[CG blk={cg.blocksRaycasts}]";
                Debug.Log($"    d={r.depth} | {FullPath(go.transform)} {tags}");
            }
            Debug.Log("[RaycastDebugger] ======================================");
        }
    }

    /// <summary>Full hierarchy path from root to this transform.</summary>
    private static string FullPath(Transform t)
    {
        var parts = new List<string>();
        Transform cur = t;
        while (cur != null) { parts.Insert(0, cur.name); cur = cur.parent; }
        return string.Join("/", parts);
    }

    /// <summary>Last `segments` segments of the hierarchy path.</summary>
    private static string ShortPath(Transform t, int segments)
    {
        var parts = new List<string>();
        Transform cur = t;
        for (int i = 0; i < segments && cur != null; i++, cur = cur.parent) parts.Insert(0, cur.name);
        return (cur != null ? ".../" : "") + string.Join("/", parts);
    }
}
