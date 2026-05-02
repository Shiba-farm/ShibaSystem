// using UnityEngine;
// using UnityEngine.EventSystems;

// public class InventorySlotClick : MonoBehaviour, IPointerClickHandler
// {
//     public InventorySlot slot;

//     public void OnPointerClick(PointerEventData eventData)
//     {
//         if (slot == null) return;

//         var drag = InventoryDragHandler.Instance;

//         // =========================
//         // A) Inventory -> Inventory
//         // =========================
//         if (drag != null && drag.IsDragging && drag.draggedFromSlot != null)
//         {
//             InventorySlot from = drag.draggedFromSlot;
//             InventorySlot to = slot;

//             if (from == null || from == to)
//             {
//                 drag.EndDrag();
//                 return;
//             }

//             // ��ͧ������ҧ -> ���·�駡ͧ
//             if (to.item == null)
//             {
//                 to.SetItem(from.item, from.amount);
//                 from.Clear();                    // << ���������
//                 drag.EndDrag();
//                 return;
//             }

//             // ��� stack (��Դ���ǡѹ + stack ��)
//             if (to.item == from.item && to.item.isStackable)
//             {
//                 int max = Mathf.Max(1, to.item.maxStack);
//                 int total = to.amount + from.amount;

//                 if (total <= max)
//                 {
//                     to.amount = total;
//                     to.UpdateUI();              // << ���������
//                     from.Clear();               // << ���������
//                 }
//                 else
//                 {
//                     to.amount = max;
//                     from.amount = total - max;
//                     from.UpdateUI();            // << ���������
//                     to.UpdateUI();              // << ���������
//                 }

//                 drag.EndDrag();
//                 return;
//             }

//             // ���Ъ�Դ -> ��Ѻ
//             ItemSO tempItem = to.item;
//             int tempAmount = to.amount;

//             to.SetItem(from.item, from.amount);

//             if (tempItem != null) from.SetItem(tempItem, tempAmount);
//             else from.Clear();

//             drag.EndDrag();
//             return;
//         }

//         // =========================
//         // B) Hotbar -> Inventory
//         // =========================
//         if (drag != null && drag.IsDragging && drag.draggedFromHotbar != null)
//         {
//             HotbarSlot hb = drag.draggedFromHotbar;
//             if (hb.item == null) { drag.EndDrag(); return; }

//             // ����� Shortcut (amount == 0) ���͹حҵ������ҧ�ͧ� inventory
//             if (!hb.HasStack) { drag.EndDrag(); return; }

//             ItemSO drop = hb.item;
//             int move = hb.amount;

//             if (slot.item == null)
//             {
//                 int setAmount = drop.isStackable ? Mathf.Min(move, drop.maxStack) : 1;
//                 slot.SetItem(drop, setAmount);

//                 hb.amount -= setAmount;
//                 if (hb.amount <= 0) hb.Clear();
//                 else hb.UpdateUI();

//                 drag.EndDrag();
//                 return;
//             }

//             // ��� stack
//             if (slot.item == drop && drop.isStackable)
//             {
//                 int max = Mathf.Max(1, drop.maxStack);
//                 int canAdd = Mathf.Min(move, max - slot.amount);
//                 if (canAdd > 0)
//                 {
//                     slot.amount += canAdd;
//                     slot.UpdateUI();

//                     hb.amount -= canAdd;
//                     if (hb.amount <= 0) hb.Clear();
//                     else hb.UpdateUI();
//                 }

//                 drag.EndDrag();
//                 return;
//             }

//             // ���Ъ�Դ -> �����Ѻ (��ա����§��áЫѺ��͹)
//             drag.EndDrag();
//             return;
//         }

//         // =========================
//         // C) ������ҡ�ҡ��ͧ Inventory
//         // =========================
//         if (slot.item != null && drag != null && !drag.IsDragging)
//         {
//             drag.BeginDrag(slot);
//         }
//     }
// }
