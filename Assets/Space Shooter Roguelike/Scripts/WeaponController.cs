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

    [Header("Visual Effects (Optional)")]
    [Tooltip("Line Renderer prefab for hitscan beam trails.")]
    public GameObject BeamTrailPrefab;
    
    [Tooltip("Impact effect for hitscan hits.")]
    public GameObject HitscanImpactPrefab;

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

    // Nested target tracking data used by the targeting system.
    // Made public so public getters can return lists of this type without accessibility errors.
    public class TargetTrackData
    {
        public Transform transform;
        public float lockTimer;
        public Vector3 predictedPos;
        public bool isLocked;
    }

    private List<TargetTrackData> activeTargets = new List<TargetTrackData>(); //All tracked targets
    private List<TargetTrackData> primaryTargets = new List<TargetTrackData>(); //For multi-lock

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
                return currentAmmo > 0; // Must have bullets left

            case AmmoSystem.HeatSink:
                return !isOverheated && currentHeat < data.MaxAmmo; // Can't fire if overheated

            case AmmoSystem.Infinite: // Always has ammo
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
                    overheatRecoveryTimer = data.OverheatPenaltyTime; // Start penalty timer
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
            ProcessFire(primaryTargets.FirstOrDefault());
        }
        else
        {
            // Multi-lock weapon: Fire at all locked targets
            var lockedTargets = primaryTargets.Where(t => t.isLocked).ToList(); 

            if (lockedTargets.Count > 0)
            {
                // Fire at each locked target
                foreach (var targetData in primaryTargets)
                {
                    ProcessFire(targetData);
                }
            }
            else
            {
                // No locks, fire forward
                ProcessFire(null);
            }
        }
    }

    // ========================================================================
    // PROCESS FIRE LOGIC
    // ========================================================================

    private void ProcessFire(TargetTrackData specificTarget)
    {
        // Calculate aim direction (shared by both methods)
        Vector3 aimDir = CalculateAimDirection(specificTarget, out Transform homingTarget);

        // Apply spread if enabled
            if (data.SpreadAngle > 0)
        {
            float x = Random.Range(-data.SpreadAngle, data.SpreadAngle);
            float y = Random.Range(-data.SpreadAngle, data.SpreadAngle);
            aimDir = Quaternion.Euler(x, y, 0) * aimDir;
        }

        // Fire multiple projectiles for shotgun-style spread
        for (int i = 0; i < data.ProjectlilesCount; i++)
        {
            // Apply individual spread per projectile
            Vector3 finalAimDir = aimDir;
            
            if (data.SpreadAngle > 0)
            {
                float x = Random.Range(-data.SpreadAngle, data.SpreadAngle);
                float y = Random.Range(-data.SpreadAngle, data.SpreadAngle);
                finalAimDir = Quaternion.Euler(x, y, 0) * aimDir;
            }

            // Choose firing method
            if (data.FiringMethod == FiringMethod.Hitscan)
            {
                FireHitscan(MuzzlePoint.position, finalAimDir);
            }
            else if (data.FiringMethod == FiringMethod.Projectile)
            {
                FireProjectile(finalAimDir, homingTarget);
            }
            // TODO: Implement FiringMethod.Hybrid
        }
    }

    // ========================================================================
    // HITSCAN FIRING
    // ========================================================================

    private void FireHitscan(Vector3 startOrigin, Vector3 aimDirection)
    {
        // Calculate maximum range
        float maxRange = (data.ProjectileLifetime > 0) 
            ? data.ProjectileSpeed * data.ProjectileLifetime 
            : 1000f;

        Vector3 currentOrigin = startOrigin;
        Vector3 currentDirection = aimDirection;
        float remainingRange = maxRange;
        
        // Track pierced targets to prevent multi-hitting
        HashSet<Collider> piercedTargets = new HashSet<Collider>();
        int remainingPierces = data.PierceCount;

        // Loop for ricochets (0 ricochets = runs once)
        for (int bounce = 0; bounce <= data.RicochetCount; bounce++)
        {
            // Raycast against enemies and ricochet surfaces
            if (Physics.Raycast(currentOrigin, currentDirection, out RaycastHit hit, 
                remainingRange, EnemyLayer | data.RicochetLayers))
            {
                // Calculate distance traveled
                float distanceTraveled = Vector3.Distance(currentOrigin, hit.point);
                
                // Spawn beam trail VFX
                SpawnBeamTrail(currentOrigin, hit.point);

                // Check if hit a damageable target
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                
                if (damageable != null && !piercedTargets.Contains(hit.collider))
                {
                    // Generate damage payload
                    DamagePayload payload = CreateDamagePayload();
                    payload.HitPoint = hit.point;
                    payload.HitNormal = hit.normal;
                    payload.DamageDirection = currentDirection;

                    // Apply damage
                    damageable.TakeDamage(payload);
                    
                    // Spawn impact VFX
                    SpawnHitscanImpact(hit.point, hit.normal);

                    // Handle piercing
                    if (remainingPierces > 0)
                    {
                        piercedTargets.Add(hit.collider);
                        remainingPierces--;
                        
                        // Continue through the target
                        remainingRange -= distanceTraveled;
                        currentOrigin = hit.point + currentDirection * 0.01f; // Small offset
                        continue;
                    }
                    else
                    {
                        // No pierces left, stop here
                        break;
                    }
                }

                // Handle explosion on impact
                if (data.ExplosionRadius > 0)
                {
                    CreateHitscanExplosion(hit.point, CreateDamagePayload());
                }

                // Handle secondary payload spawn
                if (data.PayloadTrigger == SpawnTrigger.OnImpact && data.SecondaryPrefab != null)
                {
                    SpawnSecondaryPayload(hit.point, hit.normal);
                }

                // Check for ricochet
                if (IsRicochetSurface(hit.collider))
                {
                    // Calculate reflection
                    Vector3 reflection = Vector3.Reflect(currentDirection, hit.normal);
                    
                    remainingRange -= distanceTraveled;
                    
                    if (remainingRange <= 0) break;
                    
                    currentOrigin = hit.point + hit.normal * 0.01f; // Offset from surface
                    currentDirection = reflection;
                    
                    // Optional: Add slight randomness to bounce
                    // currentDirection = Quaternion.Euler(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0) * currentDirection;
                }
                else
                {
                    // Hit non-ricochet surface, stop
                    break;
                }
            }
            else
            {
                // Missed everything, draw beam to max range
                Vector3 endPoint = currentOrigin + currentDirection * remainingRange;
                SpawnBeamTrail(currentOrigin, endPoint);
                break;
            }
        }
    }

    private bool IsRicochetSurface(Collider collider)
    {
        return ((1 << collider.gameObject.layer) & data.RicochetLayers) != 0;
    }

    private void CreateHitscanExplosion(Vector3 center, DamagePayload basePayload)
    {
        Collider[] hitColliders = Physics.OverlapSphere(center, data.ExplosionRadius, EnemyLayer);

        foreach (var hitCollider in hitColliders)
        {
            IDamageable target = hitCollider.GetComponent<IDamageable>();
            if (target != null)
            {
                // Calculate falloff
                float distance = Vector3.Distance(center, hitCollider.transform.position);
                float distanceRatio = distance / data.ExplosionRadius;
                float falloffMultiplier = 1f - (distanceRatio * distanceRatio);
                falloffMultiplier = Mathf.Max(falloffMultiplier, 0.2f);

                // Create explosion payload with falloff
                DamagePayload explosionPayload = basePayload;
                explosionPayload.PhysicalDamage *= falloffMultiplier;
                explosionPayload.EnergyDamage *= falloffMultiplier;
                explosionPayload.HitPoint = hitCollider.transform.position;
                explosionPayload.DamageDirection = (hitCollider.transform.position - center).normalized;

                // Roll crit for each target in explosion
                bool isCrit = Random.Range(0f, 100f) <= data.CritChance;
                explosionPayload.IsCritical = isCrit;
                if (isCrit)
                {
                    explosionPayload.PhysicalDamage *= data.CritMultiplier;
                    explosionPayload.EnergyDamage *= data.CritMultiplier;
                }

                target.TakeDamage(explosionPayload);
            }

            // Apply physics force
            if (data.ExplosionForce > 0)
            {
                Rigidbody rb = hitCollider.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(data.ExplosionForce, center, data.ExplosionRadius, 1f, ForceMode.Impulse);
                }
            }
        }
        
        // TODO: Spawn explosion VFX
    }

    private void SpawnSecondaryPayload(Vector3 position, Vector3 normal)
    {
        for (int i = 0; i < data.SpawnCount; i++)
        {
            Quaternion rotation = Quaternion.LookRotation(normal);

            if (data.SpawnSpreadAngle > 0)
            {
                float x = Random.Range(-data.SpawnSpreadAngle, data.SpawnSpreadAngle);
                float y = Random.Range(-data.SpawnSpreadAngle, data.SpawnSpreadAngle);
                rotation = Quaternion.Euler(x, y, 0) * rotation;
            }

            GameObject spawned = Instantiate(data.SecondaryPrefab, position, rotation);

            // Initialize if it's a SmartProjectile
            SmartProjectile subProj = spawned.GetComponent<SmartProjectile>();
            if (subProj != null)
            {
                subProj.Initialize(
                    data.SecondaryDamageStats,
                    OwnerShip,
                    data.ProjectileSpeed * 0.5f, // Half speed for secondary
                    5f,
                    null,
                    0f,
                    0, 0,
                    data.RicochetLayers,
                    data.ExplosionRadius * 0.5f,
                    0f,
                    data.ExplosionForce * 0.5f,
                    0f, 1f,
                    EnemyLayer,
                    SpawnTrigger.None,
                    0f, null, 0, 0f, false,
                    data.SecondaryDamageStats
                );
            }
        }
    }

    private void SpawnBeamTrail(Vector3 start, Vector3 end)
    {
        if (BeamTrailPrefab == null) return;

        GameObject trail = Instantiate(BeamTrailPrefab, start, Quaternion.identity);
        LineRenderer lineRenderer = trail.GetComponent<LineRenderer>();
        
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            
            // Auto-destroy after a short time
            Destroy(trail, 0.1f);
        }
    }

    private void SpawnHitscanImpact(Vector3 position, Vector3 normal)
    {
        if (HitscanImpactPrefab == null) return;

        GameObject impact = Instantiate(HitscanImpactPrefab, position, Quaternion.LookRotation(normal));
        Destroy(impact, 2f);
    }
    private void FireProjectile(Vector3 aimDir, Transform homingTarget)
    {
        for (int i = 0; i < data.ProjectlilesCount; i++)
        {
            // Instantiate projectile
            GameObject projectileObj = Instantiate(
                data.ProjectilePrefab,
                MuzzlePoint.position,
                Quaternion.LookRotation(aimDir)
            );

            // Initialize projectile with full configuration
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
    // 1. Remove destroyed/invalid targets
    for (int i = activeTargets.Count - 1; i >= 0; i--)
    {
        if (activeTargets[i].transform == null || !activeTargets[i].transform.gameObject.activeInHierarchy)
        {
            activeTargets.RemoveAt(i);
        }
    }

    // 2. Scan for potential targets (Candidates)
    Collider[] potentialEnemies = Physics.OverlapSphere(transform.position, data.MaxLockDistance, EnemyLayer);

    foreach (Collider col in potentialEnemies)
    {
        Vector3 dirToEnemy = (col.transform.position - MainCamera.transform.position).normalized;
        float angle = Vector3.Angle(MainCamera.transform.forward, dirToEnemy);

        // Only checking FOV here, NOT the Count limit!
        if (angle < 60f)
        {
            TargetTrackData existingData = activeTargets.Find(x => x.transform == col.transform);

            if (existingData == null)
            {
                // FIX: Removed the "if count < limit" check. 
                // We track everyone visible so we can decide who is best later.
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

    // 3. Clean up targets that left range/FOV
    for (int i = activeTargets.Count - 1; i >= 0; i--)
    {
        var tData = activeTargets[i];
        float dist = Vector3.Distance(transform.position, tData.transform.position);
        Vector3 dir = (tData.transform.position - MainCamera.transform.position).normalized;
        float ang = Vector3.Angle(MainCamera.transform.forward, dir);

        if (dist > data.MaxLockDistance || ang > 65f)
        {
            activeTargets.RemoveAt(i);
        }
    }

    // 4. Find primary target (closest to crosshair)
    // We do this BEFORE updating timers, so we know who to lock.
    primaryTargets.Clear(); 
    
    Vector3 aimOrigin = MainCamera.transform.position;
    Vector3 aimForward = (AimingObject.position - aimOrigin).normalized;

    var sortedTargets = activeTargets
        .Select(t => new { 
            Target = t, 
            Angle = Vector3.Angle(aimForward, (t.transform.position - aimOrigin).normalized) 
        })
        .Where(x => x.Angle < data.AssistConeAngle)
        .OrderBy(x => x.Angle)
        .Take(data.MaxLockTargets) // Only the best N targets get selected
        .Select(x => x.Target)
        .ToList();

    primaryTargets.AddRange(sortedTargets);

    // 5. Update Lock Timers (Logic Moved Here)
    foreach (var tData in activeTargets)
    {
        // Only increase timer if this specific target is in the Primary list
        if (primaryTargets.Contains(tData))
        {
            tData.lockTimer += Time.deltaTime;
            if (tData.lockTimer >= data.LockOnTimeNeeded)
            {
                tData.isLocked = true;
            }
        }
        else
        {
            // If they are no longer primary, lose the lock (or decrease timer)
            tData.lockTimer = 0f;
            tData.isLocked = false;
        }

        // Always update prediction
        Rigidbody targetRb = tData.transform.GetComponent<Rigidbody>();
        Vector3 targetVel = (targetRb != null) ? targetRb.linearVelocity : Vector3.zero;
        float dist = Vector3.Distance(transform.position, tData.transform.position);
        float travelTime = dist / data.ProjectileSpeed;
        tData.predictedPos = tData.transform.position + (targetVel * travelTime);
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
            if (primaryTargets.Contains(t))
                Gizmos.color = Color.green;
            else if (t.isLocked)
                Gizmos.color = Color.red;
            else
                Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(t.predictedPos, 1f);
            Gizmos.DrawLine(t.transform.position, t.predictedPos);
        }
    }
    public List<TargetTrackData> GetActiveTargets()
    {
        return new List<TargetTrackData>(activeTargets);
    }

    public List<TargetTrackData> GetPrimaryTargets()
    {
        return new List<TargetTrackData>(primaryTargets);
    }
}
