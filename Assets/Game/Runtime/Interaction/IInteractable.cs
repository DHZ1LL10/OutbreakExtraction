namespace Outbreak.Interaction
{
    public interface IInteractable
    {
        string Prompt { get; }
        bool CanInteract(PlayerInteractor interactor);
        void Interact(PlayerInteractor interactor);
    }

    public interface IHoldInteractable : IInteractable
    {
        float HoldDuration { get; }
        void HoldStarted(PlayerInteractor interactor);
        void HoldCancelled(PlayerInteractor interactor);
    }

    public interface IInteractionPanel
    {
        string PanelTitle { get; }
        bool IsAvailable { get; }
        void Closed(PlayerInteractor interactor);
    }
}
