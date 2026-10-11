using Outbreak.Interaction;
using UnityEngine;

namespace Outbreak.Hub
{
    public enum HubStationKind { Stash, Loadout, Trader, Workbench, Missions, Deploy }
    public abstract class HubStation : MonoBehaviour, IInteractable, ISpatialInteractionPanel
    {
        public abstract HubStationKind Kind { get; }
        public string PanelTitle => Kind == HubStationKind.Trader ? "QUARTERMASTER" : Kind == HubStationKind.Missions ? "COMMANDER" : Kind.ToString().ToUpperInvariant();
        public string Prompt => "Open " + PanelTitle;
        public bool IsAvailable => isActiveAndEnabled && HubSceneFlow.Instance != null && HubSceneFlow.Instance.Ready && !HubSceneFlow.Instance.Transitioning;
        public bool CanOpen(PlayerInteractor player) => IsAvailable && player != null && Vector3.Distance(player.transform.position, transform.position) <= 2.4f;
        public bool CanStayOpen(PlayerInteractor player) => IsAvailable && player != null && Vector3.Distance(player.transform.position, transform.position) <= 3.3f;
        public bool CanInteract(PlayerInteractor player) => CanOpen(player);
        public void Interact(PlayerInteractor player) { if (CanOpen(player)) player.OpenPanel(this); }
        public void Closed(PlayerInteractor player) { }
    }
}
