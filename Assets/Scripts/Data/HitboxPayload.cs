using UnityEngine;

 [System.Serializable]
    public struct HitboxPayload
    {
        public float Damage;
        public float KnockbackForce;
        public float LaunchForce;
        public int LaunchDir;
        public float HitstunDuration;
        public Transform attacker;

        public HitboxPayload(float damage, float force,float launch,int launchDir ,float hitstun, Transform attacker)
        {
        this.Damage = damage;
        this.KnockbackForce = force;
        this.LaunchForce = launch;
        this.LaunchDir = launchDir;
        this.HitstunDuration = hitstun;
        this.attacker = attacker;
        }
    }
