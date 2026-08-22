using System;
using Unity.Netcode;
using UnityEngine;

public class InteractController : MonoBehaviour
{
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask interactLayer;
    public static event Action<IInteractable, Transform> OnInteractableFound;
    public static event Action OnInteractableLost;
    private IInteractable currentClosest;
    private NetworkObject networkObject;
    private void Start()
    {
        networkObject = GetComponentInParent<NetworkObject>();

        // Only subscribe if this is the local player
        if (networkObject != null && !networkObject.IsOwner) return;
        if (InputHandler.Singleton != null)
        {
            InputHandler.Singleton.OnInteractTriggered += HandleInteract;
        }
    }

    private void Update()
    {
        if (networkObject != null && !networkObject.IsOwner) return;
        DetectClosest();
    }

    private void DetectClosest()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactRange, interactLayer);

        Debug.Log($"DetectClosest: Found {colliders.Length} colliders in range.");

        IInteractable closestInteractable = null;
        Transform closestTransform = null;
        float minDistance = float.MaxValue;

        foreach (var col in colliders)
        {
            if (col.TryGetComponent(out IInteractable interactable))
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestInteractable = interactable;
                    closestTransform = col.transform;
                }
            }
        }

        if (closestInteractable != currentClosest)
        {
            currentClosest = closestInteractable;

            if (currentClosest != null)
                OnInteractableFound?.Invoke(currentClosest, closestTransform);
            else
                OnInteractableLost?.Invoke();
        }
    }
    private void HandleInteract()
    {
        currentClosest?.Interact();
    }
}
