using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// ============================================================================
// WEAPON CONTROLLER - ENHANCED
// ============================================================================

/// <summary>
/// Advanced weapon controller supporting multiple firing modes, charge mechanics,
/// ammo systems, multi-target lock-on, and lead prediction.
/// Fully integrated with WeaponData ScriptableObject and SmartProjectile system.
/// </summary>
public class WeaponController : MonoBehaviour
{
    // ========================================================================
    // CONFIGURATION
    // ========================================================================

    [Header("Data Source")]
    [Tooltip("Drag the Scriptable Object here.")]
    public WeaponData data;

    [Header("Scene References")]
    [Tooltip("Where the bullet spawns.")]
    public Transform MuzzlePoint;

    [Tooltip("Used for raycasting what you are looking at.")]
    public Camera MainCamera;

    [Tooltip("Which layers count as enemies for Lock-on?")]
    public LayerMask EnemyLayer;

    [Header("Aiming Source")]
    [Tooltip("Drag the physical 3D object that represents your crosshair here.")]
    public Transform AimingObject;

    [Header("Owner")]
    [Tooltip("The ship that owns this weapon (for kill credit).")]
    public GameObject OwnerShip;

    // ========================================================================
    // STATE MACHINE VARIABLES
    // ========================================================================

    private float nextFireTime;          // Tracks cooldown between shots
    private bool isTriggerHeld;          // Is the player holding the button?
    private bool isBursting;             // Prevents interrupting a burst

    // ========================================================================
    // CHARGE SYSTEM STATE
    // ========================================================================

    private float currentChargeTime;     // Current charge progress
    private bool isCharging;             // Is weapon actively charging?
    private bool isFullyCharged;         // Has weapon reached full charge?
    private float currentSpoolFireRate;  // For SpoolUp charge style

    // ========================================================================
    // AMMO SYSTEM STATE
    // ========================================================================

    private int currentAmmo;             // Current ammo count
    private float currentHeat;           // Current heat level (for HeatSink)
    private bool isOverheated;           // Is weapon overheated?
    private bool isReloading;            // Is weapon currently reloading?
    private float overheatRecoveryTimer; // Timer for overheat penalty

    // ========================================================================
    // TARGETING STATE
    // ========================================================================

    private class TargetTrackData
    {
        public Transform transform;
        public float lockTimer;
        public Vector3 predictedPos;
        public bool isLocked;
    }

    private List<TargetTrackData> activeTargets = new List<TargetTrackData>();
    private TargetTrackData primaryTarget;

    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================

    private void Start()
    {
        InitializeWeapon();
    }

    private void Update()
    {
        // 1. Update targeting system
        HandleTargetingSystem();

        // 2. Update ammo/heat system
        HandleAmmoSystem();

        // 3. Handle charge mechanics
        HandleChargeSystem();

        // 4. Handle firing logic
        HandleFiringState();
    }

    // ========================================================================
    // INITIALIZATION
    // ========================================================================

    private void InitializeWeapon()
    {
        if (data == null)
        {
            Debug.LogError("WeaponData is not assigned!");
            return;
        }

        // Set owner if not assigned
        if (OwnerShip == null)
        {
            OwnerShip = gameObject;
        }

        // Initialize ammo
        currentAmmo = data.MaxAmmo;
        currentHeat = 0f;

        // Initialize charge
        currentChargeTime = 0f;
        isCharging = false;
        isFullyCharged = false;
        currentSpoolFireRate = data.FireRate;
    }

    // ========================================================================
    // PUBLIC INPUT METHODS
    // ========================================================================

    /// <summary>
    /// Call this when player presses fire button.
    /// </summary>
    public void StartFiring()
    {
        isTriggerHeld = true;

        // Semi-Auto fires immediately on button down
        if (data.TriggerMode == TriggerType.SemiAuto)
        {
            TryFire();
        }
    }

    /// <summary>
    /// Call this when player releases fire button.
    /// </summary>
    public void StopFiring()
    {
        isTriggerHeld = false;

        // ChargeToFire with HoldAndRelease fires on release
        if (data.TriggerMode == TriggerType.ChargeToFire)
        {
            if (data.chargeStyle == ChargeStyle.HoldAndRelease && isFullyCharged)
            {
                TryFire();
            }

            // Reset charge
            ResetCharge();
        }
    }

    /// <summary>
    /// Call this to manually reload the weapon.
    /// </summary>
    public void Reload()
    {
        if (data.ammoSystem == AmmoSystem.Magazine && !isReloading && currentAmmo < data.MaxAmmo)
        {
            StartCoroutine(ReloadRoutine());
        }
    }

    // ========================================================================
    // CHARGE SYSTEM
    // ========================================================================

    private void HandleChargeSystem()
    {
        if (data.TriggerMode != TriggerType.ChargeToFire) return;

        if (isTriggerHeld && !isBursting && !isReloading && !isOverheated)
        {
            isCharging = true;
            currentChargeTime += Time.deltaTime;

            // Handle different charge styles
            switch (data.chargeStyle)
            {
                case ChargeStyle.AutoRelease:
                    // Spartan Laser style: Auto-fire when fully charged
                    if (currentChargeTime >= data.ChargeTime)
                    {
                        isFullyCharged = true;
                        TryFire();
                        ResetCharge();
                    }
                    break;

                case ChargeStyle.HoldAndRelease:
                    // Bow/Hanzo style: Must release to fire
                    if (currentChargeTime >= data.ChargeTime)
                    {
                        isFullyCharged = true;

                        // If not allowed to hold indefinitely, auto-fire
                        if (!data.HoldToChargeIndefinitely)
                        {
                            TryFire();
                            ResetCharge();
                        }
                    }
                    break;

                case ChargeStyle.SpoolUp:
                    // Minigun style: Fire rate increases over time
                    if (currentChargeTime >= data.ChargeTime)
                    {
                        isFullyCharged = true;
                    }

                    // Increase fire rate gradually
                    float chargeProgress = Mathf.Clamp01(currentChargeTime / data.ChargeTime);
                    currentSpoolFireRate = Mathf.Lerp(data.FireRate * 0.3f, data.FireRate, chargeProgress);
                    break;
            }

            // TODO: Add visual/audio feedback
            // OnChargeProgress?.Invoke(currentChargeTime / data.ChargeTime);
        }
        else if (isCharging)
        {
            // Trigger released before full charge (except SpoolUp which keeps firing)
            if (data.chargeStyle != ChargeStyle.SpoolUp)
            {
                ResetCharge();
            }
        }
    }

    private void ResetCharge()
    {
        currentChargeTime = 0f;
        isCharging = false;
        isFullyCharged = false;
        currentSpoolFireRate = data.FireRate;
    }

    // ========================================================================
    // AMMO SYSTEM
    // ========================================================================

    private void HandleAmmoSystem()
    {
        switch (data.ammoSystem)
        {
            case AmmoSystem.Magazine:
                // Standard ammo, nothing to update here
                break;

            case AmmoSystem.HeatSink:
                // Cool down over time if not firing
                if (!isTriggerHeld && currentHeat > 0)
                {
                    currentHeat -= data.CooldownRate * Time.deltaTime;
                    currentHeat = Mathf.Max(0f, currentHeat);
                }

                // Handle overheat recovery
                if (isOverheated)
                {
                    overheatRecoveryTimer -= Time.deltaTime;
                    if (overheatRecoveryTimer <= 0f)
                    {
                        isOverheated = false;
                        currentHeat = 0f;
                    }
                }
                break;

            case AmmoSystem.Infinite:
                // No ammo management needed
                break;
        }
    }

    private bool HasAmmo()
    {
        switch (data.ammoSystem)
        {
            case AmmoSystem.Magazine:
                return currentAmmo > 0;

            case AmmoSystem.HeatSink:
                return !isOverheated && currentHeat < data.MaxAmmo;

            case AmmoSystem.Infinite:
                return true;

            default:
                return false;
        }
    }

    private void ConsumeAmmo()
    {
        switch (data.ammoSystem)
        {
            case AmmoSystem.Magazine:
                currentAmmo--;
                if (currentAmmo <= 0)
                {
                    // Auto-reload when empty
                    StartCoroutine(ReloadRoutine());
                }
                break;

            case AmmoSystem.HeatSink:
                currentHeat += data.HeatPerShot;
                if (currentHeat >= data.MaxAmmo)
                {
                    // Overheat!
                    isOverheated = true;
                    currentHeat = data.MaxAmmo;
                    overheatRecoveryTimer = data.OverheatPenaltyTime;
                }
                break;

            case AmmoSystem.Infinite:
                // No consumption
                break;
        }
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;

        // TODO: Play reload animation/sound
        // OnReloadStart?.Invoke();

        yield return new WaitForSeconds(data.ReloadTime);

        currentAmmo = data.MaxAmmo;
        isReloading = false;

        // TODO: Play reload complete sound
        // OnReloadComplete?.Invoke();
    }

    // ========================================================================
    // FIRING STATE MACHINE
    // ========================================================================

    private void HandleFiringState()
    {
        // Don't fire if bursting, reloading, or overheated
        if (isBursting || isReloading || isOverheated) return;

        // MODE: Full Auto
        if (data.TriggerMode == TriggerType.FullAuto && isTriggerHeld)
        {
            TryFire();
        }

        // MODE: ChargeToFire - SpoolUp
        // SpoolUp continues firing while held after reaching charge
        if (data.TriggerMode == TriggerType.ChargeToFire &&
            data.chargeStyle == ChargeStyle.SpoolUp &&
            isTriggerHeld &&
            isFullyCharged)
        {
            TryFire();
        }
    }

    // ========================================================================
    // FIRE EXECUTION
    // ========================================================================

    private void TryFire()
    {
        // Check cooldown
        if (Time.time < nextFireTime) return;

        // Check ammo
        if (!HasAmmo()) return;

        // Check charge requirements
        if (data.TriggerMode == TriggerType.ChargeToFire)
        {
            if (data.chargeStyle == ChargeStyle.HoldAndRelease)
            {
                // Can only fire if fully charged
                if (!isFullyCharged) return;
            }
        }

        // Set cooldown based on charge style
        float fireRate = (data.chargeStyle == ChargeStyle.SpoolUp) ? currentSpoolFireRate : data.FireRate;
        nextFireTime = Time.time + (1f / fireRate);

        // Consume ammo
        ConsumeAmmo();

        // Execute shot
        if (data.IsBurstFire)
        {
            StartCoroutine(PerformBurst());
        }
        else
        {
            ExecuteShot();
        }
    }

    private IEnumerator PerformBurst()
    {
        isBursting = true;

        for (int i = 0; i < data.BurstCount; i++)
        {
            ExecuteShot();

            // Don't wait after last bullet
            if (i < data.BurstCount - 1)
            {
                yield return new WaitForSeconds(data.BurstDelay);
            }
        }

        isBursting = false;
    }

    // ========================================================================
    // SHOT EXECUTION
    // ========================================================================

    private void ExecuteShot()
    {
        // Single target vs Multi-target logic
        if (data.MaxLockTargets == 1)
        {
            // Standard weapon: Fire at primary target or straight ahead
            SpawnProjectile(primaryTarget);
        }
        else
        {
            // Multi-lock weapon: Fire at all locked targets
            var readyTargets = activeTargets.Where(t => t.isLocked).ToList();

            if (readyTargets.Count > 0)
            {
                // Fire at each locked target
                foreach (var targetData in readyTargets)
                {
                    SpawnProjectile(targetData);
                }
            }
            else
            {
                // No locks, fire forward
                SpawnProjectile(null);
            }
        }
    }

    // ========================================================================
    // PROJECTILE SPAWNING
    // ========================================================================

    private void SpawnProjectile(TargetTrackData specificTarget)
    {
        for (int i = 0; i < data.ProjectlilesCount; i++)
        {
            // 1. Calculate aim direction
            Vector3 aimDir = CalculateAimDirection(specificTarget, out Transform homingTarget);

            // 2. Apply spread
            if (data.SpreadAngle > 0)
            {
                float x = Random.Range(-data.SpreadAngle, data.SpreadAngle);
                float y = Random.Range(-data.SpreadAngle, data.SpreadAngle);
                aimDir = Quaternion.Euler(x, y, 0) * aimDir;
            }

            // 3. Instantiate projectile
            GameObject projectileObj = Instantiate(
                data.ProjectilePrefab,
                MuzzlePoint.position,
                Quaternion.LookRotation(aimDir)
            );

            // 4. Initialize projectile with full configuration
            SmartProjectile projectileScript = projectileObj.GetComponent<SmartProjectile>();
            if (projectileScript != null)
            {
                // Create damage payload with critical hit calculation
                DamagePayload finalPayload = CreateDamagePayload();

                // Initialize with all parameters
                projectileScript.Initialize(
                    finalPayload,
                    OwnerShip,
                    data.ProjectileSpeed,
                    data.ProjectileLifetime,
                    homingTarget,
                    data.HomingTurnSpeed,
                    data.PierceCount,
                    data.RicochetCount,
                    data.RicochetLayers,
                    data.ExplosionRadius,
                    data.ProximityRadius,
                    data.ExplosionForce,
                    data.CritChance,
                    data.CritMultiplier,
                    EnemyLayer,
                    data.PayloadTrigger,
                    data.TriggerValue,
                    data.SecondaryPrefab,
                    data.SpawnCount,
                    data.SpawnSpreadAngle,
                    data.InheritVelocity,
                    data.SecondaryDamageStats
                );
            }
        }
    }

    private Vector3 CalculateAimDirection(TargetTrackData specificTarget, out Transform homingTarget)
    {
        homingTarget = null;

        if (specificTarget != null)
        {
            Vector3 directionToLead = (specificTarget.predictedPos - MuzzlePoint.position).normalized;

            // Multi-lock missiles always snap to target
            if (data.MaxLockTargets > 1)
            {
                homingTarget = specificTarget.transform;
                return directionToLead;
            }
            else
            {
                // Standard weapon with aim assist
                Vector3 dirToSphere = (AimingObject.position - MuzzlePoint.position).normalized;
                float angle = Vector3.Angle(dirToSphere, directionToLead);

                if (angle < data.AssistConeAngle)
                {
                    // Aim assist kicks in
                    if (data.IsHoming) homingTarget = specificTarget.transform;
                    return directionToLead;
                }
                else
                {
                    // Outside assist cone, aim at crosshair
                    return dirToSphere;
                }
            }
        }
        else
        {
            // No target, aim at crosshair
            return (AimingObject.position - MuzzlePoint.position).normalized;
        }
    }

    private DamagePayload CreateDamagePayload()
    {
        // Roll for critical hit
        bool isCrit = Random.Range(0f, 100f) <= data.CritChance;

        DamagePayload payload = data.PrimaryDamageStats;
        payload.Source = OwnerShip;
        payload.IsCritical = isCrit;

        if (isCrit)
        {
            payload.PhysicalDamage *= data.CritMultiplier;
            payload.EnergyDamage *= data.CritMultiplier;
        }

        return payload;
    }

    // ========================================================================
    // TARGETING SYSTEM
    // ========================================================================

    private void HandleTargetingSystem()
    {
        // 1. Remove destroyed targets
        for (int i = activeTargets.Count - 1; i >= 0; i--)
        {
            if (activeTargets[i].transform == null || !activeTargets[i].transform.gameObject.activeInHierarchy)
            {
                activeTargets.RemoveAt(i);
            }
        }

        // 2. Scan for potential targets
        Collider[] potentialEnemies = Physics.OverlapSphere(transform.position, data.MaxLockDistance, EnemyLayer);

        foreach (Collider col in potentialEnemies)
        {
            // Check if in FOV
            Vector3 dirToEnemy = (col.transform.position - MainCamera.transform.position).normalized;
            float angle = Vector3.Angle(MainCamera.transform.forward, dirToEnemy);

            if (angle < 60f)
            {
                // Check if already tracking
                TargetTrackData existingData = activeTargets.Find(x => x.transform == col.transform);

                if (existingData == null)
                {
                    // Add new target if under limit
                    if (activeTargets.Count < data.MaxLockTargets || data.MaxLockTargets == 1)
                    {
                        TargetTrackData newData = new TargetTrackData
                        {
                            transform = col.transform,
                            lockTimer = 0,
                            isLocked = false
                        };
                        activeTargets.Add(newData);
                    }
                }
            }
        }

        // 3. Update tracked targets
        for (int i = activeTargets.Count - 1; i >= 0; i--)
        {
            var tData = activeTargets[i];

            // Check if still in range and FOV
            float dist = Vector3.Distance(transform.position, tData.transform.position);
            Vector3 dir = (tData.transform.position - MainCamera.transform.position).normalized;
            float ang = Vector3.Angle(MainCamera.transform.forward, dir);

            if (dist > data.MaxLockDistance || ang > 65f)
            {
                activeTargets.RemoveAt(i);
                continue;
            }

            // Update lock timer
            tData.lockTimer += Time.deltaTime;
            if (tData.lockTimer >= data.LockOnTimeNeeded)
            {
                tData.isLocked = true;
            }

            // Update lead prediction
            Rigidbody targetRb = tData.transform.GetComponent<Rigidbody>();
            Vector3 targetVel = (targetRb != null) ? targetRb.linearVelocity : Vector3.zero;
            float travelTime = dist / data.ProjectileSpeed;
            tData.predictedPos = tData.transform.position + (targetVel * travelTime);
        }

        // 4. Find primary target (closest to crosshair)
        primaryTarget = null;
        float bestAngle = data.AssistConeAngle * 2;
        Vector3 aimDirection = (AimingObject.position - MainCamera.transform.position).normalized;

        foreach (var tData in activeTargets)
        {
            Vector3 dirToTarget = (tData.transform.position - MainCamera.transform.position).normalized;
            float angleFromCrosshair = Vector3.Angle(aimDirection, dirToTarget);

            if (angleFromCrosshair < bestAngle)
            {
                bestAngle = angleFromCrosshair;
                primaryTarget = tData;
            }
        }
    }

    // ========================================================================
    // PUBLIC GETTERS (For UI)
    // ========================================================================

    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => data.MaxAmmo;
    public float GetCurrentHeat() => currentHeat;
    public float GetMaxHeat() => data.MaxAmmo;
    public float GetChargeProgress() => currentChargeTime / data.ChargeTime;
    public bool IsFullyCharged() => isFullyCharged;
    public bool IsOverheated() => isOverheated;
    public bool IsReloading() => isReloading;
    public int GetLockedTargetCount() => activeTargets.Count(t => t.isLocked);

    // ========================================================================
    // DEBUG VISUALIZATION
    // ========================================================================

    private void OnDrawGizmos()
    {
        if (activeTargets == null) return;

        foreach (var t in activeTargets)
        {
            if (t == null || t.transform == null) continue;

            // Color code: Green = Primary, Red = Locked, Yellow = Acquiring
            if (t == primaryTarget)
                Gizmos.color = Color.green;
            else if (t.isLocked)
                Gizmos.color = Color.red;
            else
                Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(t.predictedPos, 1f);
            Gizmos.DrawLine(t.transform.position, t.predictedPos);
        }
    }
}