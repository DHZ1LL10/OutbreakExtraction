using System;
using System.Linq;
using Outbreak.Core;
using Outbreak.Items;
using Outbreak.Loadouts;
using Outbreak.Loot;
using Outbreak.Maps;
using Outbreak.Persistence;
using UnityEngine;

namespace Outbreak.Debugging
{
    [RequireComponent(typeof(GameManager))]
    public sealed class ArchitectureDebug : MonoBehaviour
    {
        [SerializeField] private ItemDefinition weapon;
        [SerializeField] private ItemDefinition medical;
        [SerializeField] private ItemDefinition valuable;
        [SerializeField] private RaidMapDefinition map;
        [SerializeField] private LootTable lootTable;
        private GameFlow Flow => GetComponent<GameManager>().Flow;

        [ContextMenu("01 - Crear partida")]
        private void NewGame() => Run(() => Require(Flow.NewGame()));
        [ContextMenu("02 - Agregar $5000")]
        private void AddMoney() => Run(() => { Hub(); Require(Flow.Profile.Wallet.Credit(5000)); });
        [ContextMenu("03 - Agregar items al stash")]
        private void AddItems() => Run(Seed);
        [ContextMenu("04 - Preparar loadout")]
        private void PrepareLoadout() => Run(Prepare);
        [ContextMenu("05 - Iniciar raid")]
        private void StartRaid() => Run(() => Require(Flow.StartRaid(map)));
        [ContextMenu("06 - Generar loot")]
        private void GenerateLoot() => Run(Loot);
        [ContextMenu("07 - Extraer exitosamente")]
        private void Extract() => Run(() => Require(Flow.ExtractSuccessfully()));
        [ContextMenu("08 - Simular muerte")]
        private void Die() => Run(() => Require(Flow.FailRaid()));
        [ContextMenu("09 - Volver al Hub")]
        private void Return() => Run(() => Require(Flow.ReturnToHub()));
        [ContextMenu("10 - Guardar")]
        private void Save() => Run(() => Require(Flow.Save()));
        [ContextMenu("11 - Cargar")]
        private void Load() => Run(() => { Flow.Load(); Debug.Log($"[OUTBREAK] {Flow.LastMessage}", this); });
        [ContextMenu("12 - Mostrar estado")]
        private void Inspect() => Run(() => { });
        [ContextMenu("Tests - Secuencia completa de extraccion")]
        private void SuccessScenario() => Run(() =>
        {
            Hub();
            Require(Flow.NewGame());
            Require(Flow.Profile.Wallet.Credit(5000));
            Seed();
            Prepare();
            Require(Flow.StartRaid(map));
            Loot();
            int found = Flow.Session.FoundLoot.Where(x => x.Item.Id == valuable.Id).Sum(x => x.Quantity);
            Require(found > 0);
            Require(Flow.ExtractSuccessfully());
            Require(Flow.Profile.Stash.GetItemCount(valuable.Id) == found);
            Require(Flow.Profile.Stash.HasItem(weapon.Id));
            Require(Flow.ReturnToHub());
            string before = JsonUtility.ToJson(SaveMapper.Capture(Flow.Profile));
            Require(Flow.Save());
            Require(Flow.Load());
            Require(before == JsonUtility.ToJson(SaveMapper.Capture(Flow.Profile)));
            Debug.Log("[OUTBREAK][PASS] Extraccion: equipo y loot en stash; dinero y perfil iguales tras guardar/cargar.", this);
        });
        [ContextMenu("Tests - Secuencia completa de muerte")]
        private void FailureScenario() => Run(() =>
        {
            Hub();
            Require(Flow.NewGame());
            Seed();
            Prepare();
            Require(Flow.StartRaid(map));
            string stashBefore = JsonUtility.ToJson(SaveMapper.Capture(Flow.Profile));
            Loot();
            var previousSession = Flow.Session;
            Require(Flow.FailRaid());
            Require(!previousSession.IsActive && previousSession.FoundLoot.Count == 0 && !previousSession.Equipment.Entries.Any());
            Require(stashBefore == JsonUtility.ToJson(SaveMapper.Capture(Flow.Profile)));
            Require(!Flow.Profile.Stash.HasItem(weapon.Id) && !Flow.Profile.Stash.HasItem(valuable.Id));
            Require(Flow.ReturnToHub());
            Require(Flow.Load());
            Require(!Flow.Profile.Stash.HasItem(weapon.Id) && !Flow.Profile.Stash.HasItem(valuable.Id));
            Debug.Log("[OUTBREAK][PASS] Muerte: equipo y loot perdidos, incluso despues de cargar.", this);
        });

        private void Seed()
        {
            Hub();
            Require(Flow.Profile.Stash.AddItem(weapon, 1));
            Require(Flow.Profile.Stash.AddItem(medical, 3));
        }
        private void Prepare()
        {
            Require(Flow.Equip(LoadoutSlot.Primary, weapon.Id));
            Require(Flow.Equip(LoadoutSlot.Equipment1, medical.Id, 2));
        }
        private void Loot()
        {
            Require(Flow.State == GameState.Raid);
            Require(Flow.Session.AddLoot(lootTable.Generate(new System.Random(42), 2, map)));
        }
        private void Hub() => Require(Flow.State == GameState.Hub);
        private void Require(bool condition)
        {
            if (!condition) throw new InvalidOperationException(Flow.LastMessage ?? "Validation failed.");
        }
        private void Run(Action action)
        {
            if (!Application.isPlaying || Flow == null)
            {
                Debug.LogWarning("[OUTBREAK] Enter Play Mode with a configured GameManager first.", this);
                return;
            }
            try
            {
                action();
                var profile = Flow.Profile;
                string stash = string.Join(", ", profile.Stash.Slots.Select(x => $"{x.Item.DisplayName} x{x.Quantity}"));
                string equipment = string.Join(", ", profile.Loadout.Entries.Select(x => $"{x.Key}: {x.Value.Item.DisplayName} x{x.Value.Quantity}"));
                Debug.Log($"[OUTBREAK] State={Flow.State}; Money=${profile.Wallet.Balance}; Level={profile.Level}; XP={profile.XP}\nStash ({profile.Stash.Slots.Count}/{profile.Stash.Capacity}): {stash}\nHub loadout: {equipment}\nRaid loot slots: {Flow.Session?.FoundLoot.Count ?? 0}", this);
            }
            catch (Exception ex) { Debug.LogError($"[OUTBREAK][FAIL] {ex.Message}", this); }
        }
    }
}
