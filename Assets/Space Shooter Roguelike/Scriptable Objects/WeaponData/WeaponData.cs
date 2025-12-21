using UnityEngine;

public enum TriggerType { SemiAuto, FullAuto, Burst, ChargeToFire }

[CreateAssetMenu(fileName = "New Weapon Config", menuName = "Weapon/Weapon Config")]
public class WeaponData : ScriptableObject
{
    [Header("Firing Logic")]
    public TriggerType TriggerMode;
    public float FireRate = 5f; // Shots per second
    public int ProjectlilesCount = 1; // Number of projectiles per shot
    public float SpreadAngle = 0f; // Degrees of random spread
    public int BurstCount = 3;         // How many bullets per burst
    public float BurstDelay = 0.1f;    // Time between bullets in a burst
    public float ChargeTime = 1.0f; // Only for 'ChargeToFire'
    public GameObject ProjectilePrefab;
    public float ProjectileSpeed = 50f;

    [Header("Targeting & Lock-on")]
    public float LockOnTimeNeeded = 1.0f; // Time required to keep target on screen
    public float MaxLockDistance = 100f;
    [Tooltip("How many enemies can we lock onto at once? 1 = Standard Gun. 5 = Swarm Missiles.")]
    public int MaxLockTargets = 1;
    
    [Header("Aim Assist")]
    [Tooltip("The angle (in degrees) within which aim assist kicks in.")]
    public float AssistConeAngle = 5.0f; 

    [Header("Advanced Projectile Behaviors")]
    public bool IsHoming;
    public float HomingTurnSpeed = 60f;
    
    [Tooltip("How many enemies can this bullet go through? 0 = Destroy on first hit.")]
    public int PierceCount = 0; 

    [Tooltip("Proximity explode distance. 0 = No proximity detonation.")]
    public float ProximityRadius = 0f;

    [Tooltip("If > 0, creates an explosion on impact.")]
    public float ExplosionRadius = 0f;
    [Tooltip("force of the explosion applied to nearby rigidbodies.")]
    public float ExplosionForce = 0f;

    [Header("Critical Hits")]
    [Range(0f, 100f)] public float CritChance = 15f; // 15% chance
    public float CritMultiplier = 2.0f; // 2x Damage

    [Header("Damage Stats")]
    public DamagePayload DamageStats; // Uses the struct we made earlier!
}