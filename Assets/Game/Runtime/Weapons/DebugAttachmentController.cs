using Outbreak.Player;
using UnityEngine;

namespace Outbreak.Weapons
{
    // Added only by the 3B test generator. No changes to permanent PlayerInput bindings.
    [DefaultExecutionOrder(150), RequireComponent(typeof(FirearmController))]
    public sealed class DebugAttachmentController : MonoBehaviour
    {
        public AttachmentDefinition redDot, suppressor, compensator, extendedMagazine, verticalGrip;
        private FirearmController firearm;
        private PlayerInput input;
        public string Status { get; private set; } = "F1 optic | F2 muzzle | F3 magazine | F4 grip";
        private void Awake() { firearm = GetComponent<FirearmController>(); input = GetComponent<PlayerInput>(); }
        private void Update()
        {
            if (input == null || !input.HasControl || firearm.Current == null) return;
            bool pressed = Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.F2) || Input.GetKeyDown(KeyCode.F3) || Input.GetKeyDown(KeyCode.F4);
            if (!pressed) return;
            if (firearm.IsSwitching || firearm.Current.IsReloading) { Status = "Espera a terminar cambio / recarga."; return; }
            bool success;
            if (Input.GetKeyDown(KeyCode.F1)) success = Toggle(redDot);
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                var muzzle = firearm.Current.Attachments[AttachmentSlot.Muzzle];
                success = muzzle == null ? firearm.Current.TryEquipAttachment(suppressor) :
                    muzzle == suppressor ? firearm.Current.TryEquipAttachment(compensator) : firearm.Current.TryRemoveAttachment(AttachmentSlot.Muzzle);
            }
            else if (Input.GetKeyDown(KeyCode.F3)) success = Toggle(extendedMagazine);
            else success = Toggle(verticalGrip);
            Status = success ? "Configuracion actualizada; ammo conservada." : "Attachment incompatible / no asignado (grip: solo rifle).";
        }
        private bool Toggle(AttachmentDefinition attachment)
        {
            if (attachment == null) return false;
            return firearm.Current.Attachments[attachment.slot] == attachment
                ? firearm.Current.TryRemoveAttachment(attachment.slot) : firearm.Current.TryEquipAttachment(attachment);
        }
    }
}
