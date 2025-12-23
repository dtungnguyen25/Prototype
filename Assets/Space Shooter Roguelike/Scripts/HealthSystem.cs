using UnityEngine;
using System;
using System.Collections;

namespace SpaceShooterRoguelike
{
    // ========================================================================
    // HEALTH SYSTEM
    // ========================================================================

    /// <summary>
    /// Three-layer damage system supporting Shield → Armor → Hull progression.
    /// Handles penetration, resistances, regeneration, and physics interactions.
    /// Fully compatible with DamagePayload system (Physical/Energy damage types).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class HealthSystem : MonoBehaviour, IDamageable
    {
        // ====================================================================
        // CONFIGURATION
        // ====================================================================

        [Header("Data Source (Optional)")]
        [Tooltip("Assign a ShipStats ScriptableObject to override the values below automatically.")]
        public ShipStats shipStats;

        // ====================================================================
        // SHIELD LAYER
        // ====================================================================

        [Header("1. Shield Layer")]
        [Tooltip("Maximum shield capacity.")]
        public float MaxShield = 100f;

        [Tooltip("Shield regenerates this many points per second.")]
        public float ShieldRegenRate = 5f;

        [Tooltip("Time in seconds before shield starts regenerating after being hit.")]
        public float ShieldRegenDelay = 3f;

        [Range(0f, 1f)]
        [Tooltip("Damage multiplier for Physical damage against shields. 0.5 = shields take 50% damage from physical.")]
        public float ShieldPhysicalIntake = 0.5f;

        // ====================================================================
        // ARMOR LAYER
        // ====================================================================

        [Header("2. Armor Layer")]
        [Tooltip("Maximum armor capacity.")]
        public float MaxArmor = 100f;

        [Range(0f, 1f)]
        [Tooltip("Damage multiplier for ALL damage hitting armor. 0.7 = armor takes 70% damage.")]
        public float ArmorDamageIntake = 0.7f;

        // ====================================================================
        // HEALTH LAYER
        // ====================================================================

        [Header("3. Health Layer")]
        [Tooltip("Maximum hull integrity (health points).")]
        public float MaxHealth = 100f;

        [Range(0f, 1f)]
        [Tooltip("Damage multiplier for Energy damage against hull. 0.5 = hull takes 50% damage from energy.")]
        public float HealthEnergyIntake = 0.5f;

        // ====================================================================
        // INTERNAL STATE
        // ====================================================================

        private Rigidbody rb;
        private Coroutine regenCoroutine;
        private WaitForSeconds regenDelayWait;

        // ====================================================================
        // PUBLIC PROPERTIES
        // ====================================================================

        /// <summary>
        /// Current shield points. Other scripts can read but not modify directly.
        /// </summary>
        public float CurrentShield { get; private set; }

        /// <summary>
        /// Current armor points. Other scripts can read but not modify directly.
        /// </summary>
        public float CurrentArmor { get; private set; }

        /// <summary>
        /// Current health points. Other scripts can read but not modify directly.
        /// </summary>
        public float CurrentHealth { get; private set; }

        // ====================================================================
        // EVENTS
        // ====================================================================

        /// <summary>
        /// Invoked when this entity dies (health reaches zero).
        /// </summary>
        public event Action OnDeath;

        /// <summary>
        /// Invoked when damage is taken. Passes the complete damage payload.
        /// </summary>
        public event Action<DamagePayload> OnDamageTaken;

        /// <summary>
        /// Invoked when shield/armor/health values change. Useful for UI updates.
        /// </summary>
        public event Action OnValuesChanged;

        // ====================================================================
        // INITIALIZATION
        // ====================================================================

        private void Awake()
        {
            // Load stats from ScriptableObject if assigned
            if (shipStats != null)
            {
                LoadStatsFromProfile(shipStats);
            }

            // Initialize current values to maximum
            CurrentShield = MaxShield;
            CurrentArmor = MaxArmor;
            CurrentHealth = MaxHealth;

            // Cache wait time for better performance
            regenDelayWait = new WaitForSeconds(ShieldRegenDelay);

            // Cache Rigidbody component for physics interactions
            rb = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// Loads all stats from a ShipStats ScriptableObject.
        /// </summary>
        private void LoadStatsFromProfile(ShipStats stats)
        {
            // Shield configuration
            MaxShield = stats.maxShield;
            ShieldRegenRate = stats.shieldRegenRate;
            ShieldRegenDelay = stats.shieldRegenDelay;
            ShieldPhysicalIntake = stats.shieldPhysicalIntake;

            // Armor configuration
            MaxArmor = stats.maxArmor;
            ArmorDamageIntake = stats.armorDamageIntake;

            // Health configuration
            MaxHealth = stats.maxHealth;
            HealthEnergyIntake = stats.healthEnergyIntake;
        }

        // ====================================================================
        // IDAMAGEABLE IMPLEMENTATION
        // ====================================================================

        /// <summary>
        /// Main damage processing function. Applies damage through Shield → Armor → Hull layers.
        /// </summary>
        public void TakeDamage(DamagePayload payload)
        {
            // 1. Extract incoming damage values
            float incomingPhys = payload.PhysicalDamage;
            float incomingNrg = payload.EnergyDamage;

            // 2. Apply impact force (knockback)
            ApplyImpactForce(payload);

            // 3. Process damage through layers
            float physPassthrough = 0f;
            float nrgPassthrough = 0f;

            // Layer 1: Shield
            ProcessShieldLayer(ref incomingPhys, ref incomingNrg, payload,
                              out physPassthrough, out nrgPassthrough);

            // Layer 2: Armor
            float finalPhysToHull = 0f;
            float finalNrgToHull = 0f;
            ProcessArmorLayer(physPassthrough, nrgPassthrough, payload,
                            out finalPhysToHull, out finalNrgToHull);

            // Layer 3: Hull
            ProcessHullLayer(finalPhysToHull, finalNrgToHull, payload);

            // 4. Post-damage handling
            HandlePostDamage(payload);
        }

        /// <summary>
        /// Returns current health as a percentage (0.0 to 1.0).
        /// </summary>
        public float GetHealthPercent()
        {
            return MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;
        }

        /// <summary>
        /// Returns true if this entity is still alive.
        /// </summary>
        public bool IsAlive()
        {
            return CurrentHealth > 0;
        }

        // ====================================================================
        // DAMAGE PROCESSING LAYERS
        // ====================================================================

        /// <summary>
        /// Applies physics impact force from the damage payload.
        /// </summary>
        private void ApplyImpactForce(DamagePayload payload)
        {
            if (rb != null && payload.ImpactForce > 0)
            {
                // Calculate push direction away from impact point
                Vector3 pushDir = (transform.position - payload.HitPoint).normalized;
                rb.AddForce(pushDir * payload.ImpactForce, ForceMode.Impulse);
            }
        }

        /// <summary>
        /// Processes damage through the shield layer, calculating penetration and overflow.
        /// </summary>
        private void ProcessShieldLayer(ref float incomingPhys, ref float incomingNrg,
                                       DamagePayload payload,
                                       out float physPassthrough, out float nrgPassthrough)
        {
            physPassthrough = 0f;
            nrgPassthrough = 0f;

            if (CurrentShield <= 0)
            {
                // Shield is down, all damage passes through
                physPassthrough = incomingPhys;
                nrgPassthrough = incomingNrg;
                return;
            }

            // A. Calculate shield penetration bypass
            float penMult = Mathf.Clamp01(payload.ShieldPenetration);
            physPassthrough = incomingPhys * penMult;
            nrgPassthrough = incomingNrg * penMult;

            // B. Calculate damage to shield (remaining portion after penetration)
            float physToShield = (incomingPhys - physPassthrough) * ShieldPhysicalIntake;
            float nrgToShield = incomingNrg - nrgPassthrough;

            // C. Apply shield damage multiplier (e.g., EMP weapons deal 2x to shields)
            float shieldMultiplier = payload.DamageToShieldsMultiplier > 0
                                   ? payload.DamageToShieldsMultiplier
                                   : 1.0f;
            float totalShieldDmg = (physToShield + nrgToShield) * shieldMultiplier;

            // D. Apply damage to shield
            if (CurrentShield >= totalShieldDmg)
            {
                // Shield absorbs all damage
                CurrentShield -= totalShieldDmg;
            }
            else
            {
                // Shield broken - calculate overflow damage
                float overflowVal = totalShieldDmg - CurrentShield;
                float overflowRatio = overflowVal / totalShieldDmg;

                CurrentShield = 0;

                // Add overflow back to passthrough damage
                physPassthrough += (incomingPhys - physPassthrough) * overflowRatio;
                nrgPassthrough += (incomingNrg - nrgPassthrough) * overflowRatio;
            }
        }

        /// <summary>
        /// Processes damage through the armor layer, calculating penetration and mitigation.
        /// </summary>
        private void ProcessArmorLayer(float physPassthrough, float nrgPassthrough,
                                      DamagePayload payload,
                                      out float finalPhysToHull, out float finalNrgToHull)
        {
            finalPhysToHull = 0f;
            finalNrgToHull = 0f;

            if (CurrentArmor <= 0 || (physPassthrough <= 0 && nrgPassthrough <= 0))
            {
                // Armor is down or no damage to process
                finalPhysToHull = physPassthrough;
                finalNrgToHull = nrgPassthrough;
                return;
            }

            // A. Calculate dynamic armor resistance based on penetration
            // Base resistance: 1.0 - ArmorDamageIntake (e.g., 0.7 intake = 0.3 or 30% resistance)
            // Armor penetration reduces this resistance proportionally
            float baseResistance = 1.0f - ArmorDamageIntake;
            float effectiveResistance = baseResistance * (1.0f - Mathf.Clamp01(payload.ArmorPenetration));
            float effectiveIntake = 1.0f - effectiveResistance;

            // B. Apply armor damage multiplier (e.g., armor-piercing rounds deal 1.5x to armor)
            float armorMult = payload.DamageToArmorMultiplier > 0
                            ? payload.DamageToArmorMultiplier
                            : 1.0f;

            // C. Calculate damage to armor after resistance
            float reducedPhys = physPassthrough * effectiveIntake;
            float reducedNrg = nrgPassthrough * effectiveIntake;
            float totalArmorDmg = (reducedPhys + reducedNrg) * armorMult;

            // D. Apply damage to armor
            if (CurrentArmor >= totalArmorDmg)
            {
                // Armor absorbs all damage
                CurrentArmor -= totalArmorDmg;
            }
            else
            {
                // Armor broken - calculate overflow damage
                float overflowVal = totalArmorDmg - CurrentArmor;
                float overflowRatio = overflowVal / totalArmorDmg;

                CurrentArmor = 0;

                // Pass mitigated damage types through based on overflow ratio
                finalPhysToHull = reducedPhys * overflowRatio;
                finalNrgToHull = reducedNrg * overflowRatio;
            }
        }

        /// <summary>
        /// Processes final damage to hull layer with energy resistance.
        /// </summary>
        private void ProcessHullLayer(float finalPhysToHull, float finalNrgToHull,
                                     DamagePayload payload)
        {
            if (finalPhysToHull <= 0 && finalNrgToHull <= 0)
                return;

            // A. Apply hull damage multiplier
            float hullMultiplier = payload.DamageToHullMultiplier > 0
                                 ? payload.DamageToHullMultiplier
                                 : 1.0f;

            // B. Apply hull energy resistance
            float finalDmg = (finalPhysToHull + (finalNrgToHull * HealthEnergyIntake)) * hullMultiplier;

            // C. Apply to health
            CurrentHealth -= finalDmg;
            CurrentHealth = Mathf.Max(0f, CurrentHealth); // Prevent negative health
        }

        // ====================================================================
        // POST-DAMAGE HANDLING
        // ====================================================================

        /// <summary>
        /// Handles events, UI updates, death checks, and shield regeneration after damage.
        /// </summary>
        private void HandlePostDamage(DamagePayload payload)
        {
            // Invoke events for UI and game systems
            OnValuesChanged?.Invoke();
            OnDamageTaken?.Invoke(payload);

            // Check for death
            if (CurrentHealth <= 0)
            {
                Die();
                return;
            }

            // Restart shield regeneration timer
            if (MaxShield > 0)
            {
                if (regenCoroutine != null)
                {
                    StopCoroutine(regenCoroutine);
                }
                regenCoroutine = StartCoroutine(RegenShieldRoutine());
            }
        }

        // ====================================================================
        // SHIELD REGENERATION
        // ====================================================================

        /// <summary>
        /// Coroutine that handles shield regeneration after a delay.
        /// </summary>
        private IEnumerator RegenShieldRoutine()
        {
            // Wait for regeneration delay (e.g., 3 seconds after taking damage)
            yield return regenDelayWait;

            // Continuously regenerate shield until full
            while (CurrentShield < MaxShield)
            {
                CurrentShield += ShieldRegenRate * Time.deltaTime;
                CurrentShield = Mathf.Min(CurrentShield, MaxShield);

                OnValuesChanged?.Invoke();

                yield return null; // Wait one frame
            }
        }

        // ====================================================================
        // DEATH HANDLING
        // ====================================================================

        /// <summary>
        /// Handles entity death, invokes events, and destroys the GameObject.
        /// </summary>
        private void Die()
        {
            OnDeath?.Invoke();

            Debug.Log($"{gameObject.name} destroyed!");

            // TODO: Replace with object pooling for better performance
            // TODO: Spawn explosion VFX here
            Destroy(gameObject);
        }

        // ====================================================================
        // PUBLIC UTILITY METHODS
        // ====================================================================

        /// <summary>
        /// Restores health points (e.g., from health pickup or repair).
        /// </summary>
        public void Repair(float amount)
        {
            CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
            OnValuesChanged?.Invoke();
        }

        /// <summary>
        /// Restores shield points.
        /// </summary>
        public void RestoreShield(float amount)
        {
            CurrentShield = Mathf.Min(CurrentShield + amount, MaxShield);
            OnValuesChanged?.Invoke();
        }

        /// <summary>
        /// Restores armor points.
        /// </summary>
        public void RestoreArmor(float amount)
        {
            CurrentArmor = Mathf.Min(CurrentArmor + amount, MaxArmor);
            OnValuesChanged?.Invoke();
        }

        /// <summary>
        /// Returns current shield as a percentage (0.0 to 1.0).
        /// </summary>
        public float GetShieldPercent()
        {
            return MaxShield > 0 ? CurrentShield / MaxShield : 0f;
        }

        /// <summary>
        /// Returns current armor as a percentage (0.0 to 1.0).
        /// </summary>
        public float GetArmorPercent()
        {
            return MaxArmor > 0 ? CurrentArmor / MaxArmor : 0f;
        }
    }
}