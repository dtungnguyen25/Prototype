using UnityEngine;

// ============================================================================
// SHIP STATS SCRIPTABLE OBJECT
// ============================================================================

/// <summary>
/// ScriptableObject containing all configuration data for a ship.
/// Defines movement, rotation, energy, and defensive stats.
/// Can be used to create different ship variants (Light Fighter, Heavy Cruiser, etc.).
/// </summary>
[CreateAssetMenu(fileName = "NewShipStats", menuName = "Ship/Ship Stats")]
public class ShipStats : ScriptableObject
{
    // ========================================================================
    // MOVEMENT CONFIGURATION
    // ========================================================================

    [Header("Movement Stats")]
    [Tooltip("Acceleration force applied when thrusting (units/s²).")]
    public float acceleration = 200f;

    [Tooltip("Maximum normal flight speed (units/s).")]
    public float maxSpeed = 70f;

    [Tooltip("Maximum speed while boosting (units/s).")]
    public float maxBoostSpeed = 200f;

    [Tooltip("Additional forward force applied during boost (units/s²).")]
    public float boostForce = 600f;

    [Tooltip("Brake strength when no input is detected. Higher = stops faster.")]
    public float brakeStrength = 5f;

    // ========================================================================
    // ROTATION CONFIGURATION
    // ========================================================================

    [Header("Rotation Stats")]
    [Tooltip("Ship turning speed (torque multiplier).")]
    public float turnSpeed = 3.5f;

    [Tooltip("Speed at which ship auto-levels to align with camera (torque multiplier).")]
    public float autoLevelSpeed = 0.5f;

    [Tooltip("Delay in seconds before auto-leveling begins after last input.")]
    public float autoLevelDelay = 2.0f;

    [Tooltip("Ship won't auto-level if moving faster than this speed (units/s).")]
    public float maxSpeedForAutoLevel = 10f;

    // ========================================================================
    // DODGE & ENERGY SYSTEM
    // ========================================================================

    [Header("Dodge & Energy")]
    [Tooltip("Impulse force applied during dodge maneuver.")]
    public float dodgeForce = 500f;

    [Tooltip("Energy cost per dodge activation.")]
    public float energyPerDodge = 25f;

    [Tooltip("Maximum energy capacity.")]
    public float maxEnergy = 100f;

    [Tooltip("Energy consumed per second while boosting.")]
    public float energyDrainRate = 20f;

    [Tooltip("Energy restored per second when not boosting.")]
    public float energyRechargeRate = 10f;

    [Tooltip("Delay in seconds before energy starts recharging after use.")]
    public float energyRechargeDelay = 2.0f;

    // ========================================================================
    // DEFENSIVE STATS (HEALTH SYSTEM INTEGRATION)
    // ========================================================================

    [Header("Defensive Stats")]

    // --- Shield Layer ---
    [Tooltip("Maximum shield capacity.")]
    public float maxShield = 100f;

    [Tooltip("Shield regenerates this many points per second.")]
    public float shieldRegenRate = 5f;

    [Tooltip("Time in seconds before shield starts regenerating after being hit.")]
    public float shieldRegenDelay = 3f;

    [Range(0f, 1f)]
    [Tooltip("Damage multiplier for Physical damage against shields. 0.5 = shields take 50% damage from physical.")]
    public float shieldPhysicalIntake = 0.5f;

    // --- Armor Layer ---
    [Tooltip("Maximum armor capacity.")]
    public float maxArmor = 100f;

    [Range(0f, 1f)]
    [Tooltip("Damage multiplier for ALL damage hitting armor. 0.7 = armor takes 70% damage.")]
    public float armorDamageIntake = 0.7f;

    // --- Health Layer ---
    [Tooltip("Maximum hull integrity (health points).")]
    public float maxHealth = 100f;

    [Range(0f, 1f)]
    [Tooltip("Damage multiplier for Energy damage against hull. 0.5 = hull takes 50% damage from energy.")]
    public float healthEnergyIntake = 0.5f;

    // ========================================================================
    // VALIDATION
    // ========================================================================

    /// <summary>
    /// Called when values change in the Inspector. Used for validation.
    /// </summary>
    private void OnValidate()
    {
        // Ensure positive values
        acceleration = Mathf.Max(0f, acceleration);
        maxSpeed = Mathf.Max(0f, maxSpeed);
        maxBoostSpeed = Mathf.Max(maxSpeed, maxBoostSpeed); // Boost should be >= normal speed
        boostForce = Mathf.Max(0f, boostForce);
        brakeStrength = Mathf.Max(0f, brakeStrength);

        turnSpeed = Mathf.Max(0f, turnSpeed);
        autoLevelSpeed = Mathf.Max(0f, autoLevelSpeed);
        autoLevelDelay = Mathf.Max(0f, autoLevelDelay);
        maxSpeedForAutoLevel = Mathf.Max(0f, maxSpeedForAutoLevel);

        dodgeForce = Mathf.Max(0f, dodgeForce);
        energyPerDodge = Mathf.Max(0f, energyPerDodge);
        maxEnergy = Mathf.Max(0f, maxEnergy);
        energyDrainRate = Mathf.Max(0f, energyDrainRate);
        energyRechargeRate = Mathf.Max(0f, energyRechargeRate);
        energyRechargeDelay = Mathf.Max(0f, energyRechargeDelay);

        maxShield = Mathf.Max(0f, maxShield);
        shieldRegenRate = Mathf.Max(0f, shieldRegenRate);
        shieldRegenDelay = Mathf.Max(0f, shieldRegenDelay);
        maxArmor = Mathf.Max(0f, maxArmor);
        maxHealth = Mathf.Max(0f, maxHealth);

        // Warn if energy per dodge exceeds max energy
        if (energyPerDodge > maxEnergy)
        {
            Debug.LogWarning($"[{name}] energyPerDodge ({energyPerDodge}) is greater than maxEnergy ({maxEnergy}). Dodge will be impossible!");
        }
    }

    // ========================================================================
    // PRESET FACTORY METHODS (OPTIONAL)
    // ========================================================================

    /// <summary>
    /// Creates a light fighter preset with high speed and maneuverability.
    /// </summary>
    public static ShipStats CreateLightFighter()
    {
        ShipStats stats = CreateInstance<ShipStats>();

        // Movement
        stats.acceleration = 250f;
        stats.maxSpeed = 90f;
        stats.maxBoostSpeed = 220f;
        stats.boostForce = 700f;
        stats.brakeStrength = 6f;

        // Rotation
        stats.turnSpeed = 5f;
        stats.autoLevelSpeed = 0.8f;

        // Energy
        stats.maxEnergy = 120f;
        stats.dodgeForce = 600f;

        // Defense (Low)
        stats.maxShield = 75f;
        stats.maxArmor = 50f;
        stats.maxHealth = 80f;

        return stats;
    }

    /// <summary>
    /// Creates a heavy cruiser preset with high durability and low speed.
    /// </summary>
    public static ShipStats CreateHeavyCruiser()
    {
        ShipStats stats = CreateInstance<ShipStats>();

        // Movement
        stats.acceleration = 150f;
        stats.maxSpeed = 50f;
        stats.maxBoostSpeed = 120f;
        stats.boostForce = 400f;
        stats.brakeStrength = 3f;

        // Rotation
        stats.turnSpeed = 2f;
        stats.autoLevelSpeed = 0.3f;

        // Energy
        stats.maxEnergy = 80f;
        stats.dodgeForce = 300f;

        // Defense (High)
        stats.maxShield = 150f;
        stats.maxArmor = 120f;
        stats.maxHealth = 150f;

        return stats;
    }
}