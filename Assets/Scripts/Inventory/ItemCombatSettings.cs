using UnityEngine;
using UnityEngine.Audio;

namespace TheSancturary.Inventory
{
    /// <summary>One balance asset shared by item definitions and both monster prefabs.</summary>
    [CreateAssetMenu(menuName = "The Sancturary/Inventory/Combat Settings")]
    public sealed class ItemCombatSettings : ScriptableObject
    {
        public InventoryItemDefinition revolver;
        [Min(1)] public int normalHitDamage = 25;
        [Min(1)] public int geoCylinderLoads = 2;
        [Min(1)] public int henryRevolverHits = 3;
        public bool shotgunInstantKill = true;
        [Min(1)] public int shotgunDamage = 300;
        [Min(0.1f)] public float sleepSeconds = 60f;
        [Min(0.1f)] public float adsWalkSpeed = 1.1f;
        [Min(0.01f)] public float adsTransitionSeconds = 0.2f;
        [Min(0.01f)] public float equipSeconds = 0.25f;
        [Min(0.1f)] public float meleeReach = 2f;
        [Min(0.01f)] public float meleeRadius = 0.16f;
        [Range(0f, 1f)] public float strikeStart = 0.42f;
        [Range(0f, 1f)] public float strikeEnd = 0.62f;
        [Min(0.1f)] public float deathVisibleSeconds = 0.8f;
        public ParticleSystem hitEffect;
        public ParticleSystem deathEffect;
        public ParticleSystem sleepEffect;
        public AudioClip[] monsterImpacts;
        public AudioClip deathBurst;
        public AudioClip dartImpact;
        public AudioMixerGroup effectsMixer;

        public int GeoHealth => ItemCombatRules.FullHealth(normalHitDamage,
            Mathf.Max(1, geoCylinderLoads) * (revolver != null ? revolver.AmmunitionCapacity : 6));
        public int HenryHealth => ItemCombatRules.FullHealth(normalHitDamage, henryRevolverHits);
    }

    public static class ItemCombatRules
    {
        public static int FullHealth(int damage, int hits) =>
            System.Math.Max(1, damage) * System.Math.Max(1, hits);

        public static int DamageResult(int health, int damage, bool instantKill) =>
            health <= 0 ? 0 : instantKill ? 0 : System.Math.Max(0, health - System.Math.Max(0, damage));

        public static bool CanStrike(float progress, float start, float end, bool alreadyHit) =>
            !alreadyHit && progress >= start && progress <= end;

        public static bool CanAim(bool living, bool restricted, bool firearm, bool busy, bool requested) =>
            living && !restricted && firearm && !busy && requested;

        public static float MovementLimit(float ordinarySpeed, float adsSpeed, bool aiming) =>
            aiming ? Mathf.Min(ordinarySpeed, Mathf.Max(0f, adsSpeed)) : ordinarySpeed;

    }
}
