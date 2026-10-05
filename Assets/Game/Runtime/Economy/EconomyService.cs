using Outbreak.Items;

namespace Outbreak.Economy
{
    public sealed class EconomyService
    {
        public bool Buy(Wallet wallet, Inventory.Inventory inventory, ItemDefinition item, int quantity)
        {
            if (item == null || quantity <= 0) return false;
            item.Validate();
            long total = (long)item.BuyValue * quantity;
            if (total > int.MaxValue || !wallet.CanAfford((int)total) || !inventory.AddItem(item, quantity)) return false;
            return wallet.Spend((int)total);
        }
        public bool Sell(Wallet wallet, Inventory.Inventory inventory, ItemDefinition item, int quantity)
        {
            if (item == null || quantity <= 0) return false;
            item.Validate();
            long total = (long)item.SellValue * quantity;
            if (total + wallet.Balance > int.MaxValue || !inventory.RemoveItem(item.Id, quantity)) return false;
            return wallet.Credit((int)total);
        }
    }
}
