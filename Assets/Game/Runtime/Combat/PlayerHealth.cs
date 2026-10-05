using UnityEngine;
namespace Outbreak.Combat
{
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : DamageableHealth
    {
        public Vector3 TargetPoint
        {
            get
            {
                var capsule = GetComponent<CharacterController>();
                return transform.TransformPoint(capsule != null ? capsule.center : Vector3.up);
            }
        }
    }
}
