using UnityEngine;

public class InteractController : MonoBehaviour
{
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask interactLayer;
    private void Start()
    {
        if (InputHandler.Singleton != null)
        {
            InputHandler.Singleton.OnInteractTriggered += HandleInteract;
        }
    }

    private void HandleInteract()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactRange, interactLayer);
        
        IInteractable closestInteractable = null;
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
                }
            }
        }

        closestInteractable?.Interact();
    }
}
