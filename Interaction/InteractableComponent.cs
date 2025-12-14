using UnityEngine;

public class InteractableComponent : MonoBehaviour, Interactable
{
    [Header("Interaction Settings")]
    public InteractionType interactionType;
    public string promptText;

    [SerializeField] private BoatController boatController;

    public void Interact()
    {
        Debug.Log("Interagiu: " + interactionType + " - " + promptText);

        switch (interactionType)
        {
            case InteractionType.Talk:
                Debug.Log("Conversando: " + promptText);
                break;

            case InteractionType.EnterBoat:
                if (boatController != null)
                {
                    boatController.EnterBoat();
                }
                else
                {
                    boatController = FindFirstObjectByType<BoatController>();
                    if (boatController != null)
                    {
                        boatController.EnterBoat();
                    }
                }
                break;

            case InteractionType.None:
                Debug.Log("Interação sem tipo definido: " + promptText);
                break;
        }
    }
}

public enum InteractionType
{
    None,
    Talk,
    EnterBoat
}