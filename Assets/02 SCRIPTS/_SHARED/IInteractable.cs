public interface IInteractable
{
    string GetInteractionPrompt();
    bool CanInteract();
    void Interact();
}
