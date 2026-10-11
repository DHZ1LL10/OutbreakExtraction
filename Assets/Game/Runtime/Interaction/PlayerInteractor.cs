using System;
using Outbreak.Player;
using Outbreak.Raids;
using UnityEngine;

namespace Outbreak.Interaction
{
    [DefaultExecutionOrder(-100), DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField, Min(0.5f)] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionMask = ~0;
        [SerializeField] private RaidRuntimeContext raidContext;
        private PlayerInput playerInput;
        private IInteractable focused;
        private IInteractable proximity;
        private IHoldInteractable activeHold;
        private float holdStartedAt;
        public IInteractable Focused => focused;
        public IInteractionPanel ActivePanel { get; private set; }
        public RaidRuntimeContext RaidContext => raidContext;
        public float HoldProgress => activeHold == null ? 0 : Mathf.Clamp01((Time.unscaledTime - holdStartedAt) / Mathf.Max(0.1f, activeHold.HoldDuration));
        public event Action FocusChanged;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            if (raidContext == null) raidContext = FindFirstObjectByType<RaidRuntimeContext>();
        }
        private void Update()
        {
            ValidatePanel();
            if (ActivePanel != null)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) ClosePanel();
                SetFocus(null); playerInput?.SetInteractionFocused(false); CancelHold(); return;
            }
            if (playerInput != null && !playerInput.HasControl) { SetFocus(null); playerInput.SetInteractionFocused(false); CancelHold(); return; }
            IInteractable candidate = ResolveRaycastTarget();
            if (candidate == null && proximity != null && proximity.CanInteract(this)) candidate = proximity;
            SetFocus(candidate);
            playerInput?.SetInteractionFocused(focused != null);
            if (ActivePanel != null) { CancelHold(); return; }
            if (focused == null) { CancelHold(); return; }
            if (focused is IHoldInteractable hold)
            {
                if (Input.GetKeyDown(KeyCode.E)) BeginHold(hold);
                if (activeHold != null && (!Input.GetKey(KeyCode.E) || activeHold != focused)) CancelHold();
                else if (activeHold != null && HoldProgress >= 1)
                {
                    var completed = activeHold; activeHold = null; completed.Interact(this);
                }
            }
            else if (Input.GetKeyDown(KeyCode.E)) focused.Interact(this);
        }
        private IInteractable ResolveRaycastTarget()
        {
            if (viewCamera == null || ActivePanel != null) return null;
            var hits = Physics.RaycastAll(viewCamera.transform.position, viewCamera.transform.forward, interactionDistance, interactionMask, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(transform)) continue;
                var behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>(true);
                foreach (var behaviour in behaviours)
                    if (behaviour is IInteractable interactable && interactable.CanInteract(this)) return interactable;
                break;
            }
            return null;
        }
        private void BeginHold(IHoldInteractable value)
        {
            if (activeHold == value) return;
            CancelHold(); activeHold = value; holdStartedAt = Time.unscaledTime; value.HoldStarted(this);
        }
        private void CancelHold()
        {
            if (activeHold == null) return;
            var cancelled = activeHold; activeHold = null; cancelled.HoldCancelled(this);
        }
        private void SetFocus(IInteractable value)
        {
            if (ReferenceEquals(focused, value)) return;
            if (activeHold != null && !ReferenceEquals(activeHold, value)) CancelHold();
            focused = value; FocusChanged?.Invoke();
        }
        public void SetProximity(IInteractable value, bool available)
        {
            if (available) proximity = value;
            else if (ReferenceEquals(proximity, value)) { proximity = null; CancelHold(); }
        }
        public void OpenPanel(IInteractionPanel panel)
        {
            if (panel == null || !panel.IsAvailable || (panel is ISpatialInteractionPanel spatial && !spatial.CanOpen(this))) return;
            if (ActivePanel != null) ClosePanel();
            ActivePanel = panel; CancelHold(); SetFocus(null); playerInput?.SetInteractionFocused(false);
            playerInput?.SetGameplaySuppressed(this, true);
        }
        public void ClosePanel()
        {
            if (ActivePanel == null) return;
            var panel = ActivePanel; ActivePanel = null;
            if (!(panel is UnityEngine.Object obj) || obj != null) panel.Closed(this);
            playerInput?.SetGameplaySuppressed(this, false);
        }
        public void ValidatePanel()
        {
            var panel = ActivePanel;
            if (panel == null) return;
            bool destroyed = panel is UnityEngine.Object obj && obj == null;
            if (destroyed || !panel.IsAvailable || (panel is ISpatialInteractionPanel spatial && !spatial.CanStayOpen(this)) ||
                (raidContext != null && raidContext.Session?.IsActive != true) || (playerInput != null && !playerInput.ControlEnabled)) ClosePanel();
        }
        private void OnDisable()
        {
            ClosePanel(); CancelHold(); playerInput?.SetInteractionFocused(false);
        }
    }
}
