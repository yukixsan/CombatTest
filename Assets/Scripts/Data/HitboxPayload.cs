using UnityEngine;

 [System.Serializable]
    public struct HitboxPayload
    {
        public float Damage;
        public float KnockbackForce;
        public float LaunchForce;
        public int LaunchDir;
        public float HitstopDuration;
    public Transform attacker;
        public int VFXindex;

        public HitboxPayload(float damage, float force,float launch,int launchDir ,float hitstop, Transform attacker,int VFXindex)
        {
        this.Damage = damage;
        this.KnockbackForce = force;
        this.LaunchForce = launch;
        this.LaunchDir = launchDir;
        this.HitstopDuration = hitstop;
        this.attacker = attacker;
        this.VFXindex = VFXindex;
        }
    }
