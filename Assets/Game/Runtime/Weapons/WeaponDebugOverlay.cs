using UnityEngine;

namespace Outbreak.Weapons
{
    [RequireComponent(typeof(FirearmController))]
    public sealed class WeaponDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool showOverlay = true;
        [SerializeField] private bool showHitmarker = true;
        [SerializeField] private bool showCrosshair = true;
        private FirearmController firearm;
        private DebugAttachmentController attachments;
        private float hitUntil, dryUntil;
        private bool headshot;
        private void Awake() { firearm = GetComponent<FirearmController>(); attachments = GetComponent<DebugAttachmentController>(); }
        private void OnEnable() { firearm.OnHit += Hit; firearm.OnDryFire += Dry; }
        private void OnDisable() { firearm.OnHit -= Hit; firearm.OnDryFire -= Dry; }
        private void Hit(DamageInfo info) { headshot = info.IsHeadshot; hitUntil = Time.unscaledTime + 0.16f; }
        private void Dry(WeaponRuntimeState state) { dryUntil = Time.unscaledTime + 0.5f; }
        private void OnGUI()
        {
            var state = firearm.Current;
            if (state == null) return;
            if (showOverlay)
            {
                float panelX = Mathf.Max(12, Screen.width - 390);
                float recoil = state.Definition.recoilVertical > 0 ? state.Stats.RecoilVertical / state.Definition.recoilVertical : 1;
                string Slot(AttachmentSlot slot) => state.Attachments[slot] != null ? state.Attachments[slot].DisplayName : "None";
                GUI.Box(new Rect(panelX, 12, 378, attachments != null ? 386 : 326), "OUTBREAK - Gunplay");
                GUI.Label(new Rect(panelX + 12, 38, 358, 354),
                    $"Weapon: {state.Definition.DisplayName}\nAmmo: {state.AmmoInMagazine} / {state.ReserveAmmo} (cap {state.Stats.MagazineCapacity})\n" +
                    $"Mode: {state.CurrentFireMode} | ADS: {state.IsAiming}\nReload: {state.IsReloading} | Switch: {firearm.IsSwitching}\nObstructed: {firearm.IsObstructed}\n" +
                    $"Optic: {Slot(AttachmentSlot.Optic)}\nMuzzle: {Slot(AttachmentSlot.Muzzle)}\nMagazine: {Slot(AttachmentSlot.Magazine)}\nGrip: {Slot(AttachmentSlot.Grip)}\n" +
                    $"Recoil V: x{recoil:F2} | Spread: {firearm.CurrentSpread:F2}\nNoise: x{state.EffectiveNoiseMultiplier:F2} | Flash: x{state.Stats.MuzzleFlashMultiplier:F2}\n" +
                    "LMB fire | RMB ADS | R reload\n1 rifle | 2 pistol | B semi/auto" +
                    (attachments != null ? "\nF1 optic | F2 muzzle | F3 mag | F4 grip\n" + attachments.Status : ""));
                if (Time.unscaledTime < dryUntil) GUI.Label(new Rect(Screen.width / 2f - 60, Screen.height / 2f + 35, 150, 25), "EMPTY MAGAZINE");
            }
            Color previous = GUI.color;
            float x = Screen.width * 0.5f, y = Screen.height * 0.5f;
            if (showCrosshair)
            {
                float gap = Mathf.Lerp(7, 2, firearm.AimWeight);
                GUI.DrawTexture(new Rect(x - gap - 5, y - 1, 5, 2), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x + gap, y - 1, 5, 2), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x - 1, y - gap - 5, 2, 5), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x - 1, y + gap, 2, 5), Texture2D.whiteTexture);
            }
            if (showHitmarker && Time.unscaledTime < hitUntil)
            {
                GUI.color = headshot ? Color.yellow : Color.white;
                GUI.Label(new Rect(x - 5, y - 10, 24, 24), "X");
                if (headshot) GUI.Label(new Rect(x - 38, y - 40, 100, 24), "HEADSHOT");
            }
            GUI.color = previous;
        }
    }
}
