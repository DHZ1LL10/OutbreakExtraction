using System;
using System.Collections.Generic;
using System.Linq;
using Outbreak.Items;
using Outbreak.Weapons;
using UnityEngine;

namespace Outbreak
{
    // Opt-in catalog data, not a second item/weapon system. No world/AI/audio changes on load.
    public sealed class ImportedContentRegistry : ScriptableObject
    {
        [Serializable] public sealed class InfectedVisualVariant
        {
            public string id;
            public GameObject model;
            public Avatar avatar;
            public RuntimeAnimatorController controller;
            public bool allowNormal,allowRunner;
        }
        [Serializable] public sealed class InfectedAudioReferences
        {
            public AudioClip[] idle,attack,damage,death;
            public AudioClip alert,chase; // No purpose-labelled clips in the free pack; intentionally null.
        }
        [Serializable] public sealed class ImpactReference { public string category; public GameObject prefab; }
        [Serializable] public sealed class AttachmentModelReference { public AttachmentDefinition attachment; public GameObject model; }
        public WeaponDefinition[] weapons=Array.Empty<WeaponDefinition>();
        public AttachmentDefinition[] attachments=Array.Empty<AttachmentDefinition>();
        public ItemDefinition[] traderItems=Array.Empty<ItemDefinition>();
        public InfectedVisualVariant[] infectedVisuals=Array.Empty<InfectedVisualVariant>();
        public InfectedAudioReferences infectedAudio=new InfectedAudioReferences();
        public ImpactReference[] futureImpacts=Array.Empty<ImpactReference>();
        public AttachmentModelReference[] futureAttachmentModels=Array.Empty<AttachmentModelReference>();
        public static T[] Merge<T>(IEnumerable<T> existing,IEnumerable<T> added) where T : UnityEngine.Object =>
            (existing??Array.Empty<T>()).Concat(added??Array.Empty<T>()).Where(item=>item!=null).Distinct().ToArray();
    }
}
