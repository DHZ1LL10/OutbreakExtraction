using Outbreak.Interaction;
using Outbreak.Raids;
using UnityEngine;

namespace Outbreak.Loot
{
    public enum LootContainerType { Toolbox, MedicalBag, Locker, MilitaryCrate, CivilianContainer }

    [DisallowMultipleComponent]
    public sealed class LootContainer : MonoBehaviour, IInteractable, ISpatialInteractionPanel
    {
        [SerializeField] private LootContainerType containerType;
        [SerializeField] private LootTable lootTable;
        [SerializeField, Range(1, 20)] private int rolls = 3;
        [SerializeField] private int seed = 7301;
        [SerializeField, Min(0.5f)] private float interactionOpenDistance = 2.4f;
        [SerializeField, Min(0.5f)] private float forcedCloseDistance = 3.3f;
        private readonly LootContainerInventory inventory = new LootContainerInventory();
        public string Prompt => "Search";
        public string PanelTitle
        {
            get
            {
                switch (containerType)
                {
                    case LootContainerType.MedicalBag: return "MEDICAL BAG";
                    case LootContainerType.MilitaryCrate: return "MILITARY CRATE";
                    case LootContainerType.CivilianContainer: return "CIVILIAN CONTAINER";
                    default: return containerType.ToString().ToUpperInvariant();
                }
            }
        }
        public bool IsAvailable => enabled && gameObject.activeInHierarchy;
        public LootContainerInventory Inventory => inventory;

        private float Distance(PlayerInteractor interactor) => Vector3.Distance(interactor.transform.position, transform.position);
        public bool CanOpen(PlayerInteractor interactor) => IsAvailable && interactor != null && Distance(interactor) <= interactionOpenDistance;
        public bool CanStayOpen(PlayerInteractor interactor) => IsAvailable && interactor != null && Distance(interactor) <= Mathf.Max(interactionOpenDistance + 0.2f, forcedCloseDistance);
        public bool CanInteract(PlayerInteractor interactor) => CanOpen(interactor) && interactor.RaidContext?.Session?.IsActive == true;
        public void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract(interactor)) return;
            EnsureGenerated(interactor.RaidContext);
            interactor.OpenPanel(this);
        }
        public void EnsureGenerated(RaidRuntimeContext context)
        {
            inventory.EnsureGenerated(() => lootTable.Generate(new System.Random(seed), rolls, context != null ? context.Map : null));
        }
        public bool TryTake(int index, RaidSession session) => inventory.TryTake(index, session);
        public int TryTakeAll(RaidSession session) => inventory.TryTakeAll(session);
        public void Closed(PlayerInteractor interactor) { }
    }
}
