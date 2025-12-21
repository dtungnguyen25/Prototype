using UnityEngine;
using System;
using System.Collections;

namespace SpaceShooterRoguelike
{
    [RequireComponent(typeof(Rigidbody))]

    public class HealthSystem : MonoBehaviour, IDamageable
{
    [Header("Data Source (Optional)")]
    [Tooltip("Assign a ShipStats object to override the values below automatically.")]
    public ShipStats shipStats;
    #region Configuration
    [Header("1. Shield Layer")]
    [Tooltip("Maximum Shield Capacity")]
    public float MaxShield = 100f;
    [Tooltip("Shield regenerates this many points per second")]
    public float ShieldRegenRate = 5f;
    [Tooltip("Time in seconds before shield starts regen after being hit")]
    public float ShieldRegenDelay = 3f;
    [Range(0f, 1f)] 
    [Tooltip("Multiplier for Physical Damage. 0.5 means shield takes 50% dmg from physical.")]
    public float ShieldPhysicalIntake = 0.5f;

    [Header("2. Armor Layer")]
    [Tooltip("Maximum Armor Capacity")]
    public float MaxArmor = 100f;
    [Range(0f, 1f)] 
    [Tooltip("Multiplier for ALL damage hitting armor. 0.7 means armor takes 70% dmg.")]
    public float ArmorDamageIntake = 0.7f;

    [Header("3. Health Layer")]
    [Tooltip("Maximum Hull Integrity")]
    public float MaxHealth = 100f;
    [Range(0f, 1f)]
    [Tooltip("Multiplier for Energy Damage. 0.5 means hull takes 50% dmg from energy.")]
    public float HealthEnergyIntake = 0.5f;
    #endregion

    private Rigidbody rb; // Reference to the Rigidbody component

    #region State
    // "Private set" means other scripts can READ these, but only THIS script can change them.
    public float CurrentShield { get; private set; }
    public float CurrentArmor { get; private set; }
    public float CurrentHealth { get; private set; }
    
    private Coroutine regenCoroutine;
    private WaitForSeconds regenDelayWait;
    #endregion

    #region Events
    // UI scripts can listen to these events to update health bars without checking every frame.
    public event Action OnDeath;
    public event Action<DamagePayload> OnDamageTaken; 
    public event Action OnValuesChanged; // Useful for UI updates
    #endregion

    private void Awake()
    {
        // Initialization
        // --- 1. LOAD STATS ---
            // If a ScriptableObject is assigned, we overwrite the Inspector defaults.
            if (shipStats != null)
            {
                LoadStatsFromProfile(shipStats);
            }
        CurrentShield = MaxShield;
        CurrentArmor = MaxArmor;
        CurrentHealth = MaxHealth;
        
        // Cache the wait time for better performance
        regenDelayWait = new WaitForSeconds(ShieldRegenDelay);
        // Cache the Rigidbody component for physics interactions
        rb = GetComponent<Rigidbody>();
    }

    private void LoadStatsFromProfile(ShipStats stats)
        {
            // Shield
            MaxShield = stats.maxShield;
            ShieldRegenRate = stats.shieldRegenRate;
            ShieldRegenDelay = stats.shieldRegenDelay;
            ShieldPhysicalIntake = stats.shieldPhysicalIntake;

            // Armor
            MaxArmor = stats.maxArmor;
            ArmorDamageIntake = stats.armorDamageIntake;

            // Health
            MaxHealth = stats.maxHealth;
            HealthEnergyIntake = stats.healthEnergyIntake;
        }

    // This is the function the Bullet calls
    public void TakeDamage(DamagePayload payload)
    {
        // 1. Initialize "Floating" Damage 
        // We keep Physical and Energy separate until the very end
        float incomingPhys = payload.PhysicalDamage;
        float incomingNrg = payload.EnergyDamage;

        // 2. Apply Physics (Impact Force)
        if (rb != null && payload.ImpactForce > 0)
        {
            // Push the object away from the bullet's impact point
            Vector3 pushDir = (transform.position - payload.HitPoint).normalized;
            rb.AddForce(pushDir * payload.ImpactForce, ForceMode.Impulse);
        }

        // --- LAYER 1: SHIELD LOGIC ---
        float physPassthrough = 0f;
        float nrgPassthrough = 0f;

        if (CurrentShield > 0)
        {
            // A. Calculate Bypass (Shield Penetration)
            // If Pen is 0.2, then 20% of damage ignores shield logic completely.
            float penMult = Mathf.Clamp01(payload.ShieldPenetration);
            
            physPassthrough = incomingPhys * penMult;
            nrgPassthrough = incomingNrg * penMult;

            // B. Calculate Damage to Shield (The remaining 80%)
            float physToShield = (incomingPhys - physPassthrough) * ShieldPhysicalIntake; // Apply Shield Resistance
            float nrgToShield = incomingNrg - nrgPassthrough; // Energy deals full dmg to shield

            // Apply Bonus Multiplier (e.g., EMP weapon does x2 dmg to shields)
            float shieldMultiplier = payload.DamageToShieldsMultiplier > 0 ? payload.DamageToShieldsMultiplier : 1.0f;
            float totalShieldDmg = (physToShield + nrgToShield) * shieldMultiplier;

            if (CurrentShield >= totalShieldDmg)
            {
                CurrentShield -= totalShieldDmg;
            }
            else
            {
                // Shield Broken! 
                // We need to calculate how much damage was NOT absorbed.
                // Simplified: We just pass the overflow percentage to the next layer.
                float overflowVal = totalShieldDmg - CurrentShield;
                float overflowRatio = overflowVal / totalShieldDmg; // e.g. 0.1 (10% overflow)
                
                CurrentShield = 0;

                // Add the overflow back to the passthrough damage
                physPassthrough += (incomingPhys - physPassthrough) * overflowRatio;
                nrgPassthrough += (incomingNrg - nrgPassthrough) * overflowRatio;
            }
        }
        else
        {
            // Shield is down, everything passes through
            physPassthrough = incomingPhys;
            nrgPassthrough = incomingNrg;
        }

        // --- LAYER 2: ARMOR LOGIC ---
        // Armor acts as a second health bar that ALSO mitigates damage.
        
        float finalPhysToHull = 0f;
        float finalNrgToHull = 0f;

        if (CurrentArmor > 0 && (physPassthrough > 0 || nrgPassthrough > 0))
        {
            // A. Calculate Dynamic Resistance
            // Base Intake 0.7 means 30% resistance.
            // If ArmorPenetration is 0.5 (50%), we cut that 30% resistance in half to 15%.
            // New Intake becomes 0.85.
            
            float baseResistance = 1.0f - ArmorDamageIntake;
            float effectiveResistance = baseResistance * (1.0f - Mathf.Clamp01(payload.ArmorPenetration));
            float effectiveIntake = 1.0f - effectiveResistance;

            // B. Apply Multipliers (e.g. Acid vs Armor)
            float armorMult = payload.DamageToArmorMultiplier > 0 ? payload.DamageToArmorMultiplier : 1.0f;

            // C. Calculate Damage to Armor
            float reducedPhys = physPassthrough * effectiveIntake;
            float reducedNrg = nrgPassthrough * effectiveIntake;
            
            float totalArmorDmg = (reducedPhys + reducedNrg) * armorMult;

            if (CurrentArmor >= totalArmorDmg)
            {
                CurrentArmor -= totalArmorDmg;
            }
            else
            {
                // Armor Broken!
                float overflowVal = totalArmorDmg - CurrentArmor;
                float overflowRatio = overflowVal / totalArmorDmg;

                CurrentArmor = 0;

                // Pass the *Original* (mitigated) damage types through based on overflow
                finalPhysToHull = reducedPhys * overflowRatio;
                finalNrgToHull = reducedNrg * overflowRatio;
            }
        }
        else
        {
            // Armor is down or bypassed
            finalPhysToHull = physPassthrough;
            finalNrgToHull = nrgPassthrough;
        }

        // --- LAYER 3: HULL LOGIC ---
        if (finalPhysToHull > 0 || finalNrgToHull > 0)
        {
            // Apply Hull Resistances (Energy reduced)
            float finalDmg = finalPhysToHull + (finalNrgToHull * HealthEnergyIntake);
            
            CurrentHealth -= finalDmg;
        }

        // --- CLEANUP ---
        HandlePostDamage(payload);
    }

    private void HandlePostDamage(DamagePayload payload)
    {
        OnValuesChanged?.Invoke();
        OnDamageTaken?.Invoke(payload); // UI can now see if (payload.IsCritical) was true!

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
        }
        else
        {
            // Interrupt Shield Regen
            if (MaxShield > 0)
            {
                if (regenCoroutine != null) StopCoroutine(regenCoroutine);
                regenCoroutine = StartCoroutine(RegenShieldRoutine());
            }
        }
    }

    private IEnumerator RegenShieldRoutine()
    {
        // Wait for the delay (e.g., 3 seconds after getting hit)
        yield return regenDelayWait;

        // Loop until shield is full
        while (CurrentShield < MaxShield)
        {
            CurrentShield += ShieldRegenRate * Time.deltaTime;
            CurrentShield = Mathf.Min(CurrentShield, MaxShield); // Clamp to max
            
            OnValuesChanged?.Invoke(); // Update UI while regenerating
            
            yield return null; // Wait for next frame
        }
    }

    private void Die()
    {
        // Send message to Game Manager or Spawner
        OnDeath?.Invoke();
        
        Debug.Log($"{gameObject.name} Exploded!");
        
        // Simple destruction (replace with object pooling later)
        Destroy(gameObject);
    }
    
        // Helper function to heal (e.g. pickup item)
    public void Repair(float amount)
        {
            CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
            OnValuesChanged?.Invoke();
        }
    }
}
