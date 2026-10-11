using Outbreak.Interaction;
using Outbreak.Inventory;
using Outbreak.Items;
using Outbreak.Raids;
using UnityEngine;

namespace Outbreak.Loot
{
    [DisallowMultipleComponent]
    public sealed class WorldItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int quantity = 1;
        public string Prompt => item == null ? "Pick Up" : "Pick Up " + item.DisplayName;
        public ItemDefinition Item => item;
        public int Quantity => quantity;
        public bool Collected { get; private set; }
        public bool CanInteract(PlayerInteractor interactor) => !Collected && item != null && interactor != null && interactor.RaidContext?.Session != null;
        public void Interact(PlayerInteractor interactor)
        {
            TryPickup(interactor.RaidContext.Session);
        }
        public bool TryPickup(RaidSession session)
        {
            if (Collected || session == null || !session.IsActive || item == null || quantity < 1 || quantity > item.MaxStack) return false;
            if (!session.AddLoot(new[] { new ItemStack(item, quantity) })) return false;
            Collected = true; gameObject.SetActive(false); return true;
        }
    }
}
