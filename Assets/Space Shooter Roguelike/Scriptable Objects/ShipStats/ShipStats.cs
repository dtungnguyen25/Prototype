using UnityEngine;

// 1. The Attribute: Adds this to the "Create" menu in Unity
[CreateAssetMenu(fileName = "NewShipStats", menuName = "Ship/Ship Stats")]
public class ShipStats : ScriptableObject // 2. Inheritance
{
    [Header("Movement Stats")]
    public float acceleration = 200f;
    public float maxSpeed = 70f;
    public float maxBoostSpeed = 200f;
    public float boostForce = 600f;
    public float brakeStrength = 5f;

    [Header("Rotation Stats")]
    public float turnSpeed = 3.5f;
    public float autoLevelSpeed = 0.5f;
    public float autoLevelDelay = 2.0f;
    public float maxSpeedForAutoLevel = 10f;

    [Header("Dodge & Energy")]
    public float dodgeForce = 500f;
    public float energyPerDodge = 25f;
    public float maxEnergy = 100f;
    public float energyDrainRate = 20f;
    public float energyRechargeRate = 10f;
    public float energyRechargeDelay = 2.0f;

    // --- NEW SECTION ---
    [Header("Defensive Stats")]
    
    // Shield
    public float maxShield = 100f;
    public float shieldRegenRate = 5f;
    public float shieldRegenDelay = 3f;
    [Range(0f, 1f)] public float shieldPhysicalIntake = 0.5f;

    // Armor
    public float maxArmor = 100f;
    [Range(0f, 1f)] public float armorDamageIntake = 0.7f;

    // Health
    public float maxHealth = 100f;
    [Range(0f, 1f)] public float healthEnergyIntake = 0.5f;
}