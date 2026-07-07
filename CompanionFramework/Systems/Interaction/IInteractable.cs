namespace Systems.Interaction
{
    public interface IInteractable
    {
        string GetPrompt();
        bool Interact();
    }
}