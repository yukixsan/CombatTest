using UnityEngine;

 [System.Serializable]
    public struct HitboxPayload
    {
        public float Damage;
        public Vector3 KnockbackDirection;
        public float KnockbackForce;
        public float HitstunDuration;

        public HitboxPayload(float damage, Vector3 direction, float force,float hitstun)
        {
            Damage = damage;
            KnockbackDirection = direction.normalized;
            KnockbackForce = force;
            HitstunDuration = hitstun;
        }
    }
