using Outbreak.Interaction;
using Outbreak.Raids;
using UnityEngine;

namespace Outbreak.Loot
{
    public enum LootContainerType { Toolbox, MedicalBag, Locker, MilitaryCrate, CivilianContainer }

    [DisallowMultipleComponent]
    public sealed class LootContainer : MonoBehaviour, IInteractable, IInteractionPanel
    {
        [SerializeField] private LootContainerType containerType;
        [SerializeField] private LootTable lootTable;
        [SerializeField, Range(1, 20)] private int rolls = 3;
        [SerializeField] private int seed = 7301;
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

        public bool CanInteract(PlayerInteractor interactor) => IsAvailable && interactor != null && interactor.RaidContext != null && interactor.RaidContext.Session != null;
        public void Interact(PlayerInteractor interactor)
        {
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
