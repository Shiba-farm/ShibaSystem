using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerItemUser : NetworkBehaviour
{
    [SerializeField] private PlayerHeldItem heldItem;
    [SerializeField] private StatManager stats;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask interactMask;
    [SerializeField] private LayerMask groundMask;
    [Header("Fishing")]
    [Tooltip("Empty child object positioned in front of the player at cast height.\n" +
             "A downward raycast from here confirms the bait would land in water.")]
    [SerializeField] private Transform fishingCastPoint;
    [Tooltip("Layer(s) that count as fishable water.")]
    [SerializeField] private LayerMask waterMask;
    [Tooltip("Max distance for the downward cast-point water check.")]
    [SerializeField] private float castCheckDistance = 6f;

    [Tooltip("เวลาสูงสุด (วินาที) ที่ InputLocked = true — ต้องมากกว่า animation ที่ยาวที่สุด")]
    [SerializeField] private float actionTimeoutSeconds = 5f;

    private bool _isActing = false;
    private Coroutine _timeoutCoroutine;
    private PlayerController _playerController;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        _playerController = GetComponent<PlayerController>();
        InputHandler.Singleton.OnLeftClick += TryUse;
        InputHandler.Singleton.OnCancelTool += TryCancelFishing;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        _playerController = null;
        InputHandler.Singleton.OnLeftClick -= TryUse;
        InputHandler.Singleton.OnCancelTool -= TryCancelFishing;
    }

    private void TryUse()
    {
        if (_isActing) return;
        // Debug.Log("Using a tool");
        if (!IsOwner) return;

        // ห้ามใช้ไอเทม/เล่น animation โจมตีตอนกำลังคุยกับ NPC อยู่ — กันปัญหาคลิกซ้าย
        // เพื่อเลื่อนบทพูดแล้วดันไปเล่น animation โจมตีด้วยพร้อมกัน (ตอนถือของ เช่น ดาบ)
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        // เช็คเพิ่ม: คลิกที่ "เพิ่งจะ" เริ่มบทสนทนา (ชี้เมาส์โดน NPC ที่คุยได้อยู่) ก็ห้ามเล่น
        // animation ด้วยเหมือนกัน แม้ IsDialogueActive ด้านบนจะยังเป็น false อยู่ก็ตาม —
        // เพราะ ณ เฟรมนี้ dialogue ยังไม่ทันเริ่ม (กำลังจะเริ่มจากคลิกเดียวกันนี้แหละ)
        if (NPCMouseInteractionUI.Instance != null && NPCMouseInteractionUI.Instance.CurrentHoverTarget != null) return;
        var data = heldItem.Current;

        if (data == null)
        {
            TryHarvest();
            return;
        }
        // Debug.Log($"Hey I want to use this {heldItem.Current}");

        if (data is not IUsable usable) return;
        if (!usable.CanUse(stats)) return;

        if (data is FishingRodSO)
        {
            if (!FaceTowardWater())
            {
                Debug.Log("[PlayerItemUser] Can't fish here — mouse is not over water.");
                return;
            }
            if (!CastPointOverWater())
            {
                Debug.Log("[PlayerItemUser] Can't fish here — cast point not over water.");
                return;
            }
        }
        else
        {
            // All other tools: snap rotation toward the farming cursor.
            // ClientNetworkTransform is client-authoritative so the rotation replicates.
            FaceTowardCursor();
        }

        _isActing = true;
        InputHandler.Singleton.InputLocked = true;
        animator.ResetTrigger(usable.AnimationHash);
        UseItemServerRpc(data.itemID);
        animator.SetTrigger(usable.AnimationHash);
        heldItem.SetHoldState(HoldState.Acting);

        FarmTileCursor.Instance?.Lock();

        // Safety: ถ้า animation event "OnActionAnimationFinished" ไม่ได้ attach ไว้ใน clip
        // (เช่น Hoe) จะล็อค input ค้าง — timeout นี้จะ force unlock หลัง N วินาที
        if (_timeoutCoroutine != null) StopCoroutine(_timeoutCoroutine);
        _timeoutCoroutine = StartCoroutine(ActionTimeoutCoroutine());
    }

    /// <summary>
    /// Called when the player presses Use with empty hands.
    /// Sends a harvest request to the server for the cell the cursor is pointing at.
    /// The server validates readiness and handles item giving + visual despawn.
    /// No animation lock is applied — harvesting is instantaneous for now.
    /// </summary>
    private void TryHarvest()
    {
        var cursor = FarmTileCursor.Instance;
        if (cursor == null || !cursor.IsOnTerrain)
        {
            Debug.Log("[PlayerItemUser] Harvest attempted but cursor is not over terrain.");
            return;
        }

        FarmingServerManager.Instance?.TryHarvestServerRpc(
            cursor.CellCoord.x,
            cursor.CellCoord.y);
    }

    /// <summary>
    /// Instantly rotates the player to face the farming cursor position (Y axis only).
    /// Falls back to the current facing direction if the cursor is not on terrain.
    /// </summary>
    private void FaceTowardCursor()
    {
        var cursor = FarmTileCursor.Instance;
        if (cursor == null || !cursor.IsOnTerrain) return;

        Vector3 dir = cursor.CellCenter - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    /// <summary>
    /// Casts a ray from the camera through the mouse position against the water layer.
    /// If it hits, rotates the player to face that point (Y-axis only) and returns true.
    /// Returns false when the mouse is not pointing at any water surface.
    ///
    /// Replaces FaceTowardCursor() for fishing rod use because FarmTileCursor only
    /// tracks terrain tiles, not the water surface.
    /// </summary>
    private bool FaceTowardWater()
    {
        if (Camera.main == null) return false;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, waterMask))
            return false;

        // Rotate the player to face the water point (Y axis only).
        Vector3 dir = hit.point - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized);

        return true;
    }

    /// <summary>
    /// After FaceTowardWater() has rotated the player, casts a ray straight down
    /// from the fishingCastPoint (child object positioned in front of the player at
    /// cast height) to confirm the bait would actually land in water — not on land
    /// just past the water's edge or a bridge railing.
    ///
    /// Returns true (allow cast) when fishingCastPoint is not assigned so the feature
    /// degrades gracefully if the Transform is forgotten in the Inspector.
    /// </summary>
    private bool CastPointOverWater()
    {
        if (fishingCastPoint == null)
        {
            Debug.LogWarning("[PlayerItemUser] fishingCastPoint is not assigned — skipping cast-point water check.");
            return true;
        }

        return Physics.Raycast(fishingCastPoint.position, Vector3.down, castCheckDistance, waterMask);
    }

    private IEnumerator ActionTimeoutCoroutine()
    {
        yield return new WaitForSeconds(actionTimeoutSeconds);
        if (_isActing)
        {
            Debug.LogWarning($"[PlayerItemUser] Animation event 'OnActionAnimationFinished' ไม่ถูกเรียก — force unlock หลัง {actionTimeoutSeconds}s");
            OnActionAnimationFinished();
        }
    }

    public void OnActionImpact()
    {
        if (!IsOwner) return;
        var data = heldItem.Current;

        // Seed planting — goes through the PlantingCursorController
        if (data is SeedItemSO)
        {
            var cursor = FarmTileCursor.Instance;
            if (cursor == null || !cursor.IsOnTerrain)
            {
                Debug.Log("[PlayerItemUser] Seed used but cursor is not over terrain.");
                return;
            }

            FarmingServerManager.Instance?.TryPlantServerRpc(
                cursor.CellCoord.x,
                cursor.CellCoord.y,
                data.itemID);
            return;
        }

        // FarmHelper (fertilizer, stat boost, etc.) — server validates tilled + range, applies effect
        if (data is FarmHelperItemSO)
        {
            var cursor = FarmTileCursor.Instance;
            if (cursor == null || !cursor.IsOnTerrain)
            {
                Debug.Log("[PlayerItemUser] FarmHelper used but cursor is not over a tilled tile.");
                return;
            }

            FarmingServerManager.Instance?.TryFertilizeServerRpc(
                cursor.CellCoord.x,
                cursor.CellCoord.y,
                data.itemID);
            return;
        }

        if (data is not ToolItemSO tool) return;

        // Hoe — ask the server to till the cell the cursor is on
        if (tool.toolTypeAction == ToolAction.Hoe)
        {
            var cursor = FarmTileCursor.Instance;
            if (cursor == null || !cursor.IsOnTerrain)
            {
                Debug.Log("[PlayerItemUser] Hoe swung but cursor is not over terrain.");
                return;
            }

            // Send grid cell coordinates (not world position) — server validates range
            FarmingServerManager.Instance?.TryTillServerRpc(
                cursor.CellCoord.x,
                cursor.CellCoord.y);

            return;
        }

        // Watering can — ask the server to water the tilled cell the cursor is on
        if (tool.toolTypeAction == ToolAction.Water)
        {
            var cursor = FarmTileCursor.Instance;
            if (cursor == null || !cursor.IsOnTerrain)
            {
                Debug.Log("[PlayerItemUser] Watering can used but cursor is not over a tilled tile.");
                return;
            }

            FarmingServerManager.Instance?.TryWaterServerRpc(
                cursor.CellCoord.x,
                cursor.CellCoord.y);

            return;
        }

        // All other tools go through the normal hit path
        HitWorldServerRpc(data.itemID);
    }

    public void OnActionAnimationFinished()
    {
        if (!_isActing) return; // ป้องกัน double-call (timeout + event ยิงพร้อมกัน)

        if (_timeoutCoroutine != null) { StopCoroutine(_timeoutCoroutine); _timeoutCoroutine = null; }

        if (heldItem.Current is FishingRodSO)
        {
            FarmTileCursor.Instance?.Unlock();
            FishingServerManager.Instance?.StartFishingServerRpc();
            return;
        }

        _isActing = false;
        InputHandler.Singleton.InputLocked = false;
        heldItem.SetHoldState(HoldState.Idle);
    }

    /// <summary>
    /// Called by PlayerHeldItem when FishingPhase returns to None (session ended).
    /// Clears the action lock so normal tool use resumes.
    /// </summary>
    public void OnFishingEnded()
    {
        _isActing = false;
        InputHandler.Singleton.InputLocked = false;
        heldItem.SetHoldState(HoldState.Idle);
    }

    /// <summary>
    /// Cancels an active fishing session (WaitingForBite phase only).
    /// Bound to OnCancelTool (right-click). Blocked by IsCriticalPanelOpen so it
    /// cannot fire while the mini-game is open — FishingMiniGameUI handles that case.
    /// </summary>
    private void TryCancelFishing()
    {
        if (heldItem.Current is not FishingRodSO) return;
        if (_playerController == null) return;
        if (_playerController.CurrentFishingPhase.Value == FishingPhase.None) return;

        // Block cancel while the mini-game panel is showing (FishBiting phase).
        // The player must finish or wait for the fish to escape — no early bail.
        if (InGameUIManager.Instance != null && InGameUIManager.Instance.IsCriticalPanelOpen) return;

        FishingServerManager.Instance?.CancelFishingServerRpc();
        // The rest of the cleanup (IsFishingIdle bool, InputLocked, _isActing)
        // flows automatically through:
        //   server CancelFishingServerRpc → SetPhase(None)
        //   → PlayerController.CurrentFishingPhase changes
        //   → PlayerHeldItem.OnFishingPhaseChanged(None)
        //   → animator.SetBool(IsFishingIdle, false) + OnFishingEnded()
    }

    // public void OnActionAnimationFinished()
    // {
    //     _isActing = false;    // ← fired by animation event at last frame
    //     heldItem.SetHoldState(HoldState.Idle);
    // }

    [ServerRpc]
    private void UseItemServerRpc(int itemID)
    {
        var data = GameDataManager.Instance.itemDatabases.GetItemByID(itemID);
        if (data is not IUsable usable) return;

        // server validates again
        if (!usable.CanUse(stats)) return;

        // consume energy — all usable items
        stats.ConsumeStat(StatType.Stamina, usable.StaminaCost);
        stats.ConsumeStat(StatType.Energy, usable.EnergyCost);

        // food specific
        if (data is FoodItemSO food)
            HealPlayer(food);
    }

    [ServerRpc]
    private void HitWorldServerRpc(int itemID)
    {
        var data = GameDataManager.Instance.itemDatabases.GetItemByID(itemID);
        if (data is not ToolItemSO tool) return;
        HitWorld(tool);
    }

    private void HitWorld(ToolItemSO tool)
    {
        var hits = Physics.OverlapSphere(
            transform.position + transform.forward,
            tool.hitRange, interactMask);

        // Debug.Log("Try to hit");

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                Debug.Log("Get the damanageable component");
                damageable.TakeDamage(tool.damage, tool.toolTypeAction);
                break;
            }
        }
    }

    /// <summary>
    /// Removes one seed from whichever inventory the owner holds it in.
    /// Must run server-side because InventoryData uses a NetworkList.
    /// </summary>
    [ServerRpc]
    private void ConsumeSeedServerRpc(int itemId)
    {
        Debug.Log($"[PlayerItemUser] ConsumeSeedServerRpc — itemId:{itemId}  ownerClient:{OwnerClientId}");

        var inventories = InventoryDataRegistry.GetAllByOwner(OwnerClientId);
        if (inventories == null)
        {
            Debug.LogWarning($"[PlayerItemUser] ConsumeSeed: no inventories found for client {OwnerClientId} in registry.");
            return;
        }

        Debug.Log($"[PlayerItemUser] ConsumeSeed: found {inventories.Count} inventories for client {OwnerClientId}.");

        foreach (var inv in inventories)
        {
            int count = inv.GetItemCount(itemId);
            Debug.Log($"[PlayerItemUser] ConsumeSeed: inv {inv.InventoryID} has {count}x item {itemId}");
            if (count > 0)
            {
                inv.RemoveItem(itemId, 1);
                Debug.Log($"[PlayerItemUser] ConsumeSeed: removed 1x item {itemId} from inv {inv.InventoryID}");
                return;
            }
        }
        Debug.LogWarning($"[PlayerItemUser] ConsumeSeed: item {itemId} not found in any inventory for client {OwnerClientId}");
    }

    private void HealPlayer(FoodItemSO food)
    {
        stats.RegenStat(StatType.Health, food.healthRestore);
        stats.RegenStat(StatType.Energy, food.energyRestore);
    }
}