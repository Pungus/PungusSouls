namespace Systems.Interaction
{
    public interface IInteractionService
    {
        bool TryInteract(IInteractable target);
    }
}