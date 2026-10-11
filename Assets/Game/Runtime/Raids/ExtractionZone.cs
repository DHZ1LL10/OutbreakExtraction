using System;
using System.Collections.Generic;
using Outbreak.Core;
using Outbreak.Interaction;
using Outbreak.Inventory;
using UnityEngine;

namespace Outbreak.Raids
{
    [RequireComponent(typeof(Collider)), DisallowMultipleComponent]
    public sealed class ExtractionZone : MonoBehaviour, IHoldInteractable
    {
        [SerializeField, Range(3, 5)] private float extractionDuration = 4f;
        [SerializeField] private string displayName = "Extraction";
        private readonly List<ItemStack> extracted = new List<ItemStack>();
        private readonly HashSet<PlayerInteractor> occupants = new HashSet<PlayerInteractor>();
        public string Prompt => "Extract";
        public float HoldDuration => extractionDuration;
        public bool Succeeded { get; private set; }
        public float CompletedAt { get; private set; }
        public float RaidDuration { get; private set; }
        public IReadOnlyList<ItemStack> Extracted => extracted.AsReadOnly();
        public int EstimatedValue { get; private set; }
        public string DisplayName => displayName;

        private void Awake() { GetComponent<Collider>().isTrigger = true; }
        public bool CanInteract(PlayerInteractor interactor) => !Succeeded && interactor != null && occupants.Contains(interactor) &&
            interactor.RaidContext?.Flow != null && interactor.RaidContext.Flow.State == GameState.Raid;
        public void HoldStarted(PlayerInteractor interactor) { }
        public void HoldCancelled(PlayerInteractor interactor) { }
        public void Interact(PlayerInteractor interactor)
        {
            var flow = interactor?.RaidContext?.Flow;
            var session = flow?.Session;
            if (!CanInteract(interactor) || flow == null || session == null || flow.State != GameState.Raid) return;
            extracted.Clear(); EstimatedValue = 0;
            foreach (var stack in session.FoundLoot)
            {
                extracted.Add(stack);
                EstimatedValue = checked(EstimatedValue + stack.Item.SellValue * stack.Quantity);
            }
            if (!flow.ExtractSuccessfully()) { extracted.Clear(); EstimatedValue = 0; return; }
            Succeeded = true; CompletedAt = Time.unscaledTime; RaidDuration = interactor.RaidContext.ElapsedRaidTime;
            var input = interactor.GetComponent<Outbreak.Player.PlayerInput>(); input?.SetControlEnabled(false);
        }
        private void OnTriggerEnter(Collider other)
        {
            var interactor = other.GetComponentInParent<PlayerInteractor>();
            if (interactor != null) { occupants.Add(interactor); interactor.SetProximity(this, true); }
        }
        private void OnTriggerExit(Collider other)
        {
            var interactor = other.GetComponentInParent<PlayerInteractor>();
            if (interactor != null) { occupants.Remove(interactor); interactor.SetProximity(this, false); }
        }
        private void OnDisable()
        {
            foreach (var interactor in occupants) if (interactor != null) interactor.SetProximity(this, false);
            occupants.Clear();
        }
    }
}
