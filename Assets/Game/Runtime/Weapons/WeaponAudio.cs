using UnityEngine;
namespace Outbreak.Weapons
{
    public enum WeaponAudioCue { Fire, DryFire, Reload, Equip }
    public static class WeaponAudio
    {
        public static AudioClip Resolve(WeaponRuntimeState state, WeaponAudioCue cue)
        {
            if (state == null) return null;
            var definition = state.Definition;
            switch (cue)
            {
                case WeaponAudioCue.Fire:
                    var muzzle = state.Attachments[AttachmentSlot.Muzzle];
                    return muzzle != null && muzzle.suppressesFireSound ? definition.suppressedFireSound : definition.fireSound;
                case WeaponAudioCue.DryFire: return definition.dryFireSound;
                case WeaponAudioCue.Reload: return definition.reloadSound;
                default: return definition.equipSound;
            }
        }
        public static void PlayOptional(AudioSource source, AudioClip clip)
        { if (source != null && clip != null) source.PlayOneShot(clip); }
    }
}
