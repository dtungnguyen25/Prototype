using UnityEngine;

// ============================================================================
// ENUMERATIONS
// ============================================================================

/// <summary>
/// Defines status effects that can be applied on hit.
/// </summary>
public enum StatusType
{
    None,
    Burn,           // Fire damage over time
    Freeze,         // Slows movement/rotation
    EMP,            // Disables weapons/abilities
    Corrosion,      // Reduces armor over time
    Stun,           // Temporary paralysis
    Slow,           // Reduces speed
    Blind,          // Reduces visibility/targeting
    Radiation       // Damages shields over time
}

// ============================================================================
// STATUS EFFECT STRUCT
// ============================================================================

/// <summary>
/// Represents a status effect that can be applied with damage.
/// </summary>
[System.Serializable]
public struct StatusEffect
{
    [Tooltip("Type of status effect to apply.")]
    public StatusType Type;

    [Tooltip("How long the effect lasts (seconds).")]
    public float Duration;

    [Tooltip("Damage per second while effect is active (0 for non-damaging effects).")]
    public float TickDamage;

    [Tooltip("Movement speed multiplier (0.5 = 50% speed, only for Slow/Freeze).")]
    [Range(0f, 1f)]
    public float MovementMultiplier;

    [Tooltip("Can this effect stack multiple times?")]
    public bool CanStack;

    [Tooltip("Maximum stack count if stacking is enabled.")]
    public int MaxStacks;
}

// ============================================================================
// DAMAGE PAYLOAD STRUCT
// ============================================================================

/// <summary>
/// Complete damage information package passed from attacker to target.
/// Supports dual damage types (Physical/Energy), penetration, status effects, critical hits, and more.
/// </summary>
[System.Serializable]
public struct DamagePayload
{
    // ========================================================================
    // BASE DAMAGE VALUES
    // ========================================================================

    [Header("Base Values")]
    [Tooltip("Physical damage component (kinetic, ballistic, explosive).")]
    public float PhysicalDamage;

    [Tooltip("Energy damage component (lasers, plasma, electromagnetic).")]
    public float EnergyDamage;

    // ========================================================================
    // PENETRATION SYSTEM
    // ========================================================================

    [Header("Penetration (0.0 to 1.0)")]
    [Tooltip("Percentage of shield defense ignored. 1.0 = Ignore 100% of shields.")]
    [Range(0f, 1f)]
    public float ShieldPenetration;

    [Tooltip("Percentage of armor defense ignored. 1.0 = Ignore 100% of armor.")]
    [Range(0f, 1f)]
    public float ArmorPenetration;

    // ========================================================================
    // DAMAGE MULTIPLIERS
    // ========================================================================

    [Header("Bonuses (Multipliers)")]
    [Tooltip("Damage multiplier against shields. 2.0 = Double damage to shields. Default: 1.0")]
    public float DamageToShieldsMultiplier;

    [Tooltip("Damage multiplier against armor. 2.0 = Double damage to armor. Default: 1.0")]
    public float DamageToArmorMultiplier;

    [Tooltip("Damage multiplier against hull/health. 1.5 = 50% bonus to hull. Default: 1.0")]
    public float DamageToHullMultiplier;

    // ========================================================================
    // CRITICAL HITS & WEAKPOINTS
    // ========================================================================

    [Header("Critical Hits & Weakpoints")]
    [Tooltip("Is this hit a critical hit? (Set by weapon system)")]
    public bool IsCritical;

    [Tooltip("Did this hit strike a weakpoint? (Set by collision detection)")]
    public bool HitWeakpoint;

    [Tooltip("Additional damage multiplier for weakpoint hits. Default: 1.0")]
    public float WeakpointMultiplier;

    // ========================================================================
    // STATUS EFFECTS
    // ========================================================================

    [Header("Status Effects")]
    [Tooltip("Array of status effects to apply on hit.")]
    public StatusEffect[] AppliedEffects;

    // ========================================================================
    // PHYSICS & KNOCKBACK
    // ========================================================================

    [Header("Physics & Feedback")]
    [Tooltip("Force applied to rigidbody on direct hit (knockback).")]
    public float ImpactForce;

    [Tooltip("Direction of the damage (normalized). Used for directional shields/armor.")]
    public Vector3 DamageDirection;

    // ========================================================================
    // LIFESTEAL & VAMPIRISM
    // ========================================================================

    [Header("Lifesteal")]
    [Tooltip("Percentage of damage dealt returned as health to attacker. 0.2 = 20% lifesteal.")]
    [Range(0f, 1f)]
    public float LifestealPercent;

    // ========================================================================
    // METADATA
    // ========================================================================

    [Header("Metadata")]
    [Tooltip("The GameObject that fired this shot (for kill credit, vampirism, team checking).")]
    public GameObject Source;

    [Tooltip("Exact world position where the hit occurred (for VFX spawning).")]
    public Vector3 HitPoint;

    [Tooltip("Surface normal at hit point (for impact effects orientation).")]
    public Vector3 HitNormal;

    // ========================================================================
    // HELPER METHODS
    // ========================================================================

    /// <summary>
    /// Returns the total raw damage (Physical + Energy) before any modifiers.
    /// </summary>
    public float GetTotalRawDamage()
    {
        return PhysicalDamage + EnergyDamage;
    }

    /// <summary>
    /// Creates a copy of this payload with modified damage values.
    /// Useful for falloff calculations without modifying original.
    /// </summary>
    public DamagePayload WithDamageMultiplier(float multiplier)
    {
        DamagePayload copy = this;
        copy.PhysicalDamage *= multiplier;
        copy.EnergyDamage *= multiplier;
        return copy;
    }

    /// <summary>
    /// Checks if this damage has any status effects to apply.
    /// </summary>
    public bool HasStatusEffects()
    {
        return AppliedEffects != null && AppliedEffects.Length > 0;
    }

    /// <summary>
    /// Gets the dominant damage type based on which value is higher.
    /// Returns "Physical", "Energy", or "Hybrid".
    /// </summary>
    public string GetDominantDamageType()
    {
        if (PhysicalDamage > EnergyDamage)
            return "Physical";
        else if (EnergyDamage > PhysicalDamage)
            return "Energy";
        else
            return "Hybrid";
    }

    /// <summary>
    /// Returns the ratio of Physical damage to total damage (0.0 to 1.0).
    /// Useful for VFX blending or damage calculations.
    /// </summary>
    public float GetPhysicalRatio()
    {
        float total = GetTotalRawDamage();
        return total > 0 ? PhysicalDamage / total : 0f;
    }

    /// <summary>
    /// Returns the ratio of Energy damage to total damage (0.0 to 1.0).
    /// Useful for VFX blending or damage calculations.
    /// </summary>
    public float GetEnergyRatio()
    {
        float total = GetTotalRawDamage();
        return total > 0 ? EnergyDamage / total : 0f;
    }
}

// ============================================================================
// DAMAGE INTERFACE
// ============================================================================

/// <summary>
/// Interface for any object that can receive damage.
/// Allows weapons to hit Asteroids, Ships, Turrets, etc. without knowing their type.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Called when this object takes damage.
    /// </summary>
    /// <param name="payload">Complete damage information package.</param>
    void TakeDamage(DamagePayload payload);

    /// <summary>
    /// Gets the current health percentage (0-1).
    /// Useful for UI health bars, AI targeting priority, etc.
    /// </summary>
    float GetHealthPercent();

    /// <summary>
    /// Checks if this damageable is still alive/active.
    /// </summary>
    bool IsAlive();
}

// ============================================================================
// USAGE EXAMPLES (COMMENTED)
// ============================================================================

/*
 * EXAMPLE 1: Basic Ballistic Weapon (Heavy Physical)
 * ===================================================
 * 
 * DamagePayload bulletDamage = new DamagePayload
 * {
 *     PhysicalDamage = 35f,
 *     EnergyDamage = 0f,
 *     ShieldPenetration = 0.1f,
 *     ArmorPenetration = 0.3f,
 *     DamageToShieldsMultiplier = 0.8f,  // Weak vs shields
 *     DamageToArmorMultiplier = 1.3f,    // Strong vs armor
 *     DamageToHullMultiplier = 1.0f,
 *     ImpactForce = 150f,
 *     Source = playerShip
 * };
 * 
 * // Result: Kinetic weapon good against armor, weak against shields
 * 
 * 
 * EXAMPLE 2: Laser Weapon (Pure Energy)
 * ======================================
 * 
 * DamagePayload laserDamage = new DamagePayload
 * {
 *     PhysicalDamage = 0f,
 *     EnergyDamage = 40f,
 *     ShieldPenetration = 0.2f,
 *     ArmorPenetration = 0.1f,
 *     DamageToShieldsMultiplier = 1.5f,  // Strong vs shields
 *     DamageToArmorMultiplier = 0.7f,    // Weak vs armor
 *     DamageToHullMultiplier = 1.0f,
 *     ImpactForce = 50f,
 *     Source = playerShip
 * };
 * 
 * // Result: Energy weapon excels against shields, struggles with armor
 * 
 * 
 * EXAMPLE 3: Plasma Cannon (Hybrid with Burn)
 * ============================================
 * 
 * DamagePayload plasmaDamage = new DamagePayload
 * {
 *     PhysicalDamage = 20f,
 *     EnergyDamage = 30f,
 *     ShieldPenetration = 0.3f,
 *     ArmorPenetration = 0.2f,
 *     DamageToShieldsMultiplier = 1.3f,
 *     DamageToArmorMultiplier = 1.1f,
 *     DamageToHullMultiplier = 1.0f,
 *     AppliedEffects = new StatusEffect[]
 *     {
 *         new StatusEffect
 *         {
 *             Type = StatusType.Burn,
 *             Duration = 3f,
 *             TickDamage = 5f,
 *             CanStack = true,
 *             MaxStacks = 3
 *         }
 *     },
 *     LifestealPercent = 0.15f, // 15% healing
 *     ImpactForce = 100f,
 *     Source = playerShip
 * };
 * 
 * // Result: Balanced hybrid weapon with burn DOT and lifesteal
 * 
 * 
 * EXAMPLE 4: EMP Missile (Energy-based Disabler)
 * ===============================================
 * 
 * DamagePayload empDamage = new DamagePayload
 * {
 *     PhysicalDamage = 15f,
 *     EnergyDamage = 85f,
 *     ShieldPenetration = 0.9f,          // Bypasses shields
 *     ArmorPenetration = 0.3f,
 *     DamageToShieldsMultiplier = 2.5f,  // Destroys shields
 *     DamageToArmorMultiplier = 0.8f,
 *     DamageToHullMultiplier = 0.9f,
 *     AppliedEffects = new StatusEffect[]
 *     {
 *         new StatusEffect
 *         {
 *             Type = StatusType.EMP,
 *             Duration = 5f,
 *             TickDamage = 0f,            // No DOT, just disables
 *             CanStack = false
 *         }
 *     },
 *     ImpactForce = 300f,
 *     Source = playerShip
 * };
 * 
 * // Result: Shield buster that disables systems
 * 
 * 
 * EXAMPLE 5: Cryo Weapon (Energy with Freeze)
 * ============================================
 * 
 * DamagePayload cryoDamage = new DamagePayload
 * {
 *     PhysicalDamage = 5f,
 *     EnergyDamage = 35f,
 *     ShieldPenetration = 0.2f,
 *     ArmorPenetration = 0.15f,
 *     DamageToShieldsMultiplier = 1.1f,
 *     DamageToArmorMultiplier = 0.9f,
 *     DamageToHullMultiplier = 1.0f,
 *     AppliedEffects = new StatusEffect[]
 *     {
 *         new StatusEffect
 *         {
 *             Type = StatusType.Freeze,
 *             Duration = 4f,
 *             TickDamage = 2f,
 *             MovementMultiplier = 0.3f,  // 70% movement slow
 *             CanStack = true,
 *             MaxStacks = 2
 *         }
 *     },
 *     ImpactForce = 80f,
 *     Source = playerShip
 * };
 * 
 * // Result: Crowd control weapon, slows and damages over time
 * 
 * 
 * EXAMPLE 6: Railgun (Armor-Piercing Physical)
 * =============================================
 * 
 * DamagePayload railgunDamage = new DamagePayload
 * {
 *     PhysicalDamage = 150f,
 *     EnergyDamage = 0f,
 *     ShieldPenetration = 0.5f,
 *     ArmorPenetration = 0.9f,           // Almost ignores armor
 *     DamageToShieldsMultiplier = 0.9f,
 *     DamageToArmorMultiplier = 1.6f,    // Devastating vs armor
 *     DamageToHullMultiplier = 1.3f,
 *     IsCritical = true,                 // High crit chance
 *     WeakpointMultiplier = 2.5f,        // Massive weakpoint bonus
 *     ImpactForce = 800f,
 *     Source = playerShip
 * };
 * 
 * // Result: Heavy sniper, punches through armor, huge weakpoint damage
 * 
 * 
 * EXAMPLE 7: Corrosive Weapon (Physical DOT)
 * ===========================================
 * 
 * DamagePayload corrosiveDamage = new DamagePayload
 * {
 *     PhysicalDamage = 25f,
 *     EnergyDamage = 15f,
 *     ShieldPenetration = 0.3f,
 *     ArmorPenetration = 0.6f,
 *     DamageToArmorMultiplier = 1.2f,
 *     AppliedEffects = new StatusEffect[]
 *     {
 *         new StatusEffect
 *         {
 *             Type = StatusType.Corrosion,
 *             Duration = 8f,
 *             TickDamage = 4f,
 *             CanStack = true,
 *             MaxStacks = 5               // Long-lasting stackable DOT
 *         }
 *     },
 *     LifestealPercent = 0.2f,            // 20% sustain
 *     ImpactForce = 70f,
 *     Source = playerShip
 * };
 * 
 * // Result: Sustain weapon, stacks corrosion, heals attacker
 */