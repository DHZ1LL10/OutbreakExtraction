using System;
using Outbreak.Items;
using UnityEngine;

namespace Outbreak
{
    // Presentation-only lookup. ItemDefinition remains the sole item identity.
    public sealed class ItemPreviewLibrary : ScriptableObject
    {
        [Serializable] public sealed class Entry { public ItemDefinition item; public GameObject model; }
        public Entry[] entries=Array.Empty<Entry>();
        public GameObject Find(ItemDefinition item) { foreach(var e in entries) if(e.item==item) return e.model; return null; }
    }
}
