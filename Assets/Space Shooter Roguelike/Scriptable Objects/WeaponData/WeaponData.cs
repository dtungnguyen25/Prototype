using UnityEngine;

// ============================================================================
// ENUMERATIONS
// ============================================================================

/// <summary>
/// Defines how the weapon fires when the player interacts with the fire button.
/// </summary>
public enum TriggerType
{
    SemiAuto,      // Single shot per click
    FullAuto,      // Hold to keep firing
    ChargeToFire   // Hold to charge, release to fire
}

/// <summary>
/// Defines charging behavior for weapons with TriggerType.ChargeToFire.
/// </summary>
public enum ChargeStyle
{
    None,           // No charging behavior
    AutoRelease,    // Hold → Wait Time → Fires automatically (e.g., Spartan Laser)
    HoldAndRelease, // Hold → Wait Time → Player releases button → Fires (e.g., Bow/Hanzo)
    SpoolUp         // Hold → Fire Rate increases over time (e.g., Minigun/Gatling)
}

/// <summary>
/// Determines the projectile delivery system used by the weapon.
/// </summary>
public enum FiringMethod
{
    Projectile,     // Spawns a GameObject that moves through space
    Hitscan,        // Instant Raycast (e.g., Sniper/Soldier: 76)
    Hybrid          // Instant beam trace + Projectile (e.g., Apex Charge Rifle)
}

/// <summary>
/// Defines the ammunition/resource management system for the weapon.
/// </summary>
public enum AmmoSystem
{
    Magazine,   // Standard COD style: Shoot → Reload → Full again
    HeatSink,   // Halo Plasma/Overwatch D.Va: Shoot → Heat Up → Cooldown (or Overheat)
    Infinite    // Arcade style, no ammo constraints
}

/// <summary>
/// Determines when secondary payloads/spawned objects are triggered.
/// </summary>
public enum SpawnTrigger
{
    None,        // No secondary spawn
    OnImpact,    // Spawns when hitting a wall/enemy (e.g., Cluster Grenade)
    OnDistance,  // Spawns after traveling X meters (e.g., Roadhog)
    OnTimer,     // Spawns after X seconds (e.g., Airburst Flak Cannon)
    OnDeath      // Spawns when the projectile is destroyed in any way
}

// ============================================================================
// WEAPON DATA SCRIPTABLE OBJECT
// ============================================================================

/// <summary>
/// ScriptableObject containing all configuration data for a weapon system.
/// Used to define weapon behavior, stats, and special mechanics for a space shooter.
/// </summary>
[CreateAssetMenu(fileName = "New Weapon Config", menuName = "Weapon/Weapon Config")]
public class WeaponData : ScriptableObject
{
    // ========================================================================
    // FIRING LOGIC
    // ========================================================================

    [Header("Firing Logic")]
    [Tooltip("Determines how the weapon responds to fire input (semi-auto, full-auto, charge).")]
    public TriggerType TriggerMode;

    [Tooltip("Type of firing logic used by the weapon (projectile, hitscan, hybrid).")]
    public FiringMethod FiringMethod;

    [Tooltip("If true, weapon fires in bursts rather than continuous shots.")]
    public bool IsBurstFire = false;

    [Tooltip("Rate of fire in shots per second.")]
    public float FireRate = 5f;

    [Tooltip("Number of projectiles spawned per shot (shotgun pellets, multi-shot).")]
    public int ProjectlilesCount = 1;

    [Tooltip("Degrees of random spread applied to each projectile.")]
    public float SpreadAngle = 0f;

    [Tooltip("Number of bullets fired per burst (only applies if IsBurstFire is true).")]
    public int BurstCount = 3;

    [Tooltip("Time delay between bullets in a burst (seconds).")]
    public float BurstDelay = 0.1f;

    [Tooltip("Charging behavior style (only applies when TriggerMode is ChargeToFire).")]
    public ChargeStyle chargeStyle;

    [Tooltip("Time required to fully charge the weapon (seconds).")]
    public float ChargeTime = 1.0f;

    [Tooltip("If true, player can hold charge beyond ChargeTime for indefinite duration.")]
    public bool HoldToChargeIndefinitely = false;

    [Tooltip("Initial velocity of spawned projectiles (units per second).")]
    public float ProjectileSpeed = 50f;

    [Tooltip("Time before projectile self-destructs (0 = infinite).")]
    public float ProjectileLifetime = 10f;

    [Tooltip("Prefab to instantiate when firing (must have projectile script attached).")]
    public GameObject ProjectilePrefab;

    [Header("Ammo System")]
    [Tooltip("Type of ammunition/resource management used by the weapon.")]
    public AmmoSystem ammoSystem;
    [Tooltip("Maximum ammunition capacity (for Magazine and HeatSink systems).")]
    public int MaxAmmo = 30;
    [Tooltip("Current ammunition count at start (for Magazine and HeatSink systems).")]
    public int CurrentAmmo = 30;
    [Tooltip("Time required to reload or cool down (seconds).")]
    public float ReloadTime = 2.0f;
    [Tooltip("Heat generated per shot (for HeatSink system).")]
    public float HeatPerShot = 10f;        // For HeatSink
    [Tooltip("Rate at which weapon cools down (units per second).")]
    public float CooldownRate = 20f;       // For HeatSink
    [Tooltip("Time weapon remains unusable after overheating (seconds).")]
    public float OverheatPenaltyTime = 3f; // For HeatSink

    // ========================================================================
    // TARGETING & LOCK-ON
    // ========================================================================

    [Header("Targeting & Lock-on")]
    [Tooltip("Time required to maintain target in crosshair to achieve lock (seconds).")]
    public float LockOnTimeNeeded = 1.0f;

    [Tooltip("Maximum distance at which targets can be locked onto (units).")]
    public float MaxLockDistance = 100f;

    [Tooltip("Maximum number of simultaneous lock targets. 1 = Standard Gun, 5+ = Swarm Missiles.")]
    public int MaxLockTargets = 1;

    // ========================================================================
    // AIM ASSIST
    // ========================================================================

    [Header("Aim Assist")]
    [Tooltip("Angular cone (degrees) within which aim assist magnetism is active.")]
    public float AssistConeAngle = 5.0f;

    // ========================================================================
    // ADVANCED PROJECTILE BEHAVIORS
    // ========================================================================

    [Header("Advanced Projectile Behaviors")]
    [Tooltip("If true, projectile will track and follow locked targets.")]
    public bool IsHoming;

    [Tooltip("Rotation speed of homing projectiles (degrees per second).")]
    public float HomingTurnSpeed = 60f;

    [Tooltip("Number of times the projectile can bounce off surfaces/ enemies before being destroyed.")]
    public int RicochetCount = 0;

    [Tooltip("Layers that trigger a ricochet bounce (typically Ground/Walls).")]
    public LayerMask RicochetLayers;

    [Tooltip("Number of enemies this projectile can penetrate. 0 = Destroy on first hit.")]
    public int PierceCount = 0;

    [Tooltip("Distance at which projectile detonates near enemies (0 = disabled).")]
    public float ProximityRadius = 0f;

    [Tooltip("Radius of explosion effect on impact (0 = no explosion).")]
    public float ExplosionRadius = 0f;

    [Tooltip("Force magnitude applied to rigidbodies within explosion radius.")]
    public float ExplosionForce = 0f;

    // ========================================================================
    // CRITICAL HITS
    // ========================================================================

    [Header("Critical Hits")]
    [Range(0f, 100f)]
    [Tooltip("Percentage chance (0-100) to deal critical damage.")]
    public float CritChance = 15f;

    [Tooltip("Damage multiplier applied on critical hits (e.g., 2.0 = double damage).")]
    public float CritMultiplier = 2.0f;

    // ========================================================================
    // DAMAGE STATS
    // ========================================================================

    [Header("Damage Stats")]
    [Tooltip("Primary damage configuration applied to direct hits.")]
    public DamagePayload PrimaryDamageStats;

    // ========================================================================
    // SECONDARY PAYLOAD (SPAWNING ON EVENT)
    // ========================================================================

    [Header("Secondary Payload (Spawning X on Y)")]
    [Tooltip("Event trigger that spawns secondary objects (impact, distance, timer, etc.).")]
    public SpawnTrigger PayloadTrigger;

    [Tooltip("Trigger threshold value:\n• OnDistance: Meters traveled\n• OnTimer: Seconds elapsed")]
    public float TriggerValue = 10.0f;

    [Tooltip("Prefab to spawn on trigger (can be missile, mine, shrapnel, etc.).")]
    public GameObject SecondaryPrefab;

    [Tooltip("Number of secondary objects to spawn on trigger.")]
    public int SpawnCount = 1;

    [Tooltip("Angular spread (degrees) for spawned objects distribution (360 = full sphere).")]
    public float SpawnSpreadAngle = 360f;

    [Tooltip("If true, spawned objects inherit velocity from parent projectile.")]
    public bool InheritVelocity = true;

    [Header("Damage")]
    [Tooltip("Damage configuration for spawned secondary projectiles.")]
    public DamagePayload SecondaryDamageStats;
}