using System;

namespace Outbreak.Economy
{
    public sealed class Wallet
    {
        public int Balance { get; private set; }
        public Wallet(int balance = 0)
        {
            if (balance < 0) throw new ArgumentOutOfRangeException(nameof(balance));
            Balance = balance;
        }
        public bool CanAfford(int amount) => amount >= 0 && Balance >= amount;
        public bool Credit(int amount)
        {
            if (amount < 0 || (long)Balance + amount > int.MaxValue) return false;
            Balance += amount;
            return true;
        }
        public bool Spend(int amount)
        {
            if (!CanAfford(amount)) return false;
            Balance -= amount;
            return true;
        }
    }
}
