using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class WeaponController : MonoBehaviour
{
    // --- CONFIGURATION ---
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

    // --- STATE MACHINE VARIABLES ---
    private float nextFireTime;          // Tracks cooldown between shots
    private bool isTriggerHeld;          // Is the player holding the button?
    private float currentChargeTime;     // For ChargeToFire mode
    private bool isBursting;             // Prevents interrupting a burst

// --- TARGETING STATE (NEW) ---
    // We now store a LIST of targets, not just one.
    private class TargetTrackData
    {
        public Transform transform;
        public float lockTimer;
        public Vector3 predictedPos;
        public bool isLocked; // Is the timer full?
    }

    private List<TargetTrackData> activeTargets = new List<TargetTrackData>();
    private TargetTrackData primaryTarget; // The specific target closest to crosshair
    // --- UNITY METHODS ---

    private void Update()
    {
        // 1. CONSTANTLY SCAN FOR TARGETS
        // We run this every frame to update the Lead Prediction and Lock-on status
        HandleTargetingSystem();

        // 2. HANDLE FIRING LOGIC
        HandleFiringState();
    }

    // --- PUBLIC INPUT METHODS (Call these from your Player Input script) ---

    public void StartFiring()
    {
        Debug.Log("1. StartFiring called!");
        isTriggerHeld = true;

        // If Semi-Auto, we try to fire immediately on the "Down" press
        if (data.TriggerMode == TriggerType.SemiAuto)
        {
            TryFire();
        }
    }

    public void StopFiring()
    {
        isTriggerHeld = false;
        
        // If we release the button, reset the charge
        currentChargeTime = 0; 
    }

    // --- CORE LOGIC ---

    private void HandleFiringState()
    {
        // If we are mid-burst, do not allow other firing logic to run
        if (isBursting) return;

        // MODE: Full Auto
        // As long as trigger is held, keep trying to fire
        if (data.TriggerMode == TriggerType.FullAuto && isTriggerHeld)
        {
            TryFire();
        }

        // MODE: Charge To Fire
        // Increase charge while held. Fire when maxed.
        if (data.TriggerMode == TriggerType.ChargeToFire && isTriggerHeld)
        {
            currentChargeTime += Time.deltaTime;
            
            // TODO: Add visual/audio feedback here (e.g., sound pitch rising)
            
            if (currentChargeTime >= data.ChargeTime)
            {
                TryFire();
                currentChargeTime = 0; // Reset after firing
            }
        }
    }

    // Checks Cooldowns before allowing a shot
    private void TryFire()
    {
        // Check if we are still cooling down
        if (Time.time < nextFireTime) return;

        // Set the cooldown for the NEXT shot
        nextFireTime = Time.time + (1f / data.FireRate);

        if (data.IsBurstFire == true)
        {
            StartCoroutine(PerformBurst());
        }
        else
        {
            ExecuteShot();
        }
    }

    // Coroutine for Burst Fire (Fire -> Wait -> Fire -> Wait)
    private IEnumerator PerformBurst()
    {
        isBursting = true; // Lock the gun so it can't fire other modes

        for (int i = 0; i < data.BurstCount; i++)
        {
            ExecuteShot();
            // Wait for the small delay between burst bullets
            yield return new WaitForSeconds(data.BurstDelay);
        }

        isBursting = false; // Unlock
    }

// --- THE NEW FIRING LOGIC ---
    private void ExecuteShot()
    {
        Debug.Log("3. Executing Shot logic..."); // DEBUG
        // LOGIC BRANCH: Single Target vs Multi-Target Weapon
        
        if (data.MaxLockTargets == 1)
        {
            // STANDARD GUN: Fires 1 stream at the Primary Target (or straight ahead)
            SpawnProjectile(primaryTarget);
        }
        else
        {
            // MULTI-LOCK WEAPON (Missile Swarm): Fires at ALL locked targets
            // 1. Get all targets that are fully locked
            var readyTargets = activeTargets.Where(t => t.isLocked).ToList();

            if (readyTargets.Count > 0)
            {
                // Fire at everyone!
                foreach (var targetData in readyTargets)
                {
                    SpawnProjectile(targetData);
                }
            }
            else
            {
                // No locks yet? Fire one dumb fire shot forward
                SpawnProjectile(null);
            }
        }
    }

    // Refactored aiming/spawning into a helper function
    private void SpawnProjectile(TargetTrackData specificTarget)
    {
        for (int i = 0; i < data.ProjectlilesCount; i++)
        {
            // 1. Aim Logic
            Vector3 aimDir = MuzzlePoint.forward;
            Transform homingTargetTransform = null;

            if (specificTarget != null)
            {
                // We have a specific target (either Primary or one of the swarm)
                Vector3 directionToLead = (specificTarget.predictedPos - MuzzlePoint.position).normalized;
                
                // For Multi-Lock missiles, we ALWAYS aim at the target.
                // For Standard Gun, we check aiming cone (Aim Assist).
                if (data.MaxLockTargets > 1)
                {
                    aimDir = directionToLead; // Always snap for missiles
                    homingTargetTransform = specificTarget.transform;
                }
                else
                {      
                    // Gun Logic: Check Aim Assist Cone
                    // We compare the direction to the ENEMY vs the direction to the SPHERE
                    Vector3 dirToSphere = (AimingObject.position - MuzzlePoint.position).normalized;
                    float angle = Vector3.Angle(dirToSphere, directionToLead);

                    if (angle < data.AssistConeAngle) aimDir = directionToLead;
                    else aimDir = dirToSphere; // Aim at the sphere if assist fails

                    if (data.IsHoming) homingTargetTransform = specificTarget.transform;
                }
            }
            else
            {
                // NO TARGET: Aim directly at the 3D Sphere
                aimDir = (AimingObject.position - MuzzlePoint.position).normalized;
            }
            // 2. Spread
            if (data.SpreadAngle > 0)
            {
                float x = Random.Range(-data.SpreadAngle, data.SpreadAngle);
                float y = Random.Range(-data.SpreadAngle, data.SpreadAngle);
                aimDir = Quaternion.Euler(x, y, 0) * aimDir;
            }

            // 3. Instantiate
            GameObject projectileObj = Instantiate(data.ProjectilePrefab, MuzzlePoint.position, Quaternion.LookRotation(aimDir));

            // 4. Initialize
            SmartProjectile projectileScript = projectileObj.GetComponent<SmartProjectile>();
            if (projectileScript != null)
            {
                // (Your existing Crit calculation here...)
                bool isCrit = Random.Range(0f, 100f) <= data.CritChance;
                DamagePayload finalPayload = data.DamageStats;
                if (isCrit)
                {
                    finalPayload.IsCritical = true;
                    finalPayload.PhysicalDamage *= data.CritMultiplier;
                    finalPayload.EnergyDamage *= data.CritMultiplier;
                }

                projectileScript.Initialize(
                    finalPayload,
                    data.ProjectileSpeed,
                    5.0f,
                    homingTargetTransform, // Pass the specific target!
                    data.HomingTurnSpeed,
                    data.PierceCount,
                    data.ExplosionRadius,
                    data.ProximityRadius,
                    data.ExplosionForce,
                    EnemyLayer
                );
            }
        }
    }

    // --- THE NEW MULTI-TARGETING SYSTEM ---

    // --- AIMING & MATH ---

    // --- TARGETING SYSTEM ---

private void HandleTargetingSystem()
    {
        // 1. CLEANUP: Remove null targets (destroyed enemies)
        for (int i = activeTargets.Count - 1; i >= 0; i--)
        {
            if (activeTargets[i].transform == null || !activeTargets[i].transform.gameObject.activeInHierarchy)
            {
                activeTargets.RemoveAt(i);
            }
        }

        // 2. SCAN: Find all valid targets in view
        Collider[] potentialEnemies = Physics.OverlapSphere(transform.position, data.MaxLockDistance, EnemyLayer);
        
        foreach (Collider col in potentialEnemies)
        {
            // A. Check Angle
            Vector3 dirToEnemy = (col.transform.position - MainCamera.transform.position).normalized;
            float angle = Vector3.Angle(MainCamera.transform.forward, dirToEnemy);

            // 60 degree field of view for locking
            if (angle < 60f) 
            {
                // B. Check if we already track this guy
                TargetTrackData existingData = activeTargets.Find(x => x.transform == col.transform);

                if (existingData == null)
                {
                    // New target found! Add if we haven't hit the cap
                    if (activeTargets.Count < data.MaxLockTargets || data.MaxLockTargets == 1)
                    {
                        // Note: If MaxTargets is 1, we just add them all to the list anyway 
                        // and let the "Primary Target" logic pick the best one. 
                        // This allows switching targets smoothly.
                        TargetTrackData newData = new TargetTrackData();
                        newData.transform = col.transform;
                        newData.lockTimer = 0;
                        activeTargets.Add(newData);
                    }
                }
            }
        }

        // 3. UPDATE & PRUNE: Calculate predictions and timers
        // Also remove targets that went off-screen (angle > 60) or too far
        for (int i = activeTargets.Count - 1; i >= 0; i--)
        {
            var tData = activeTargets[i];
            
            // Re-check distance and angle
            float dist = Vector3.Distance(transform.position, tData.transform.position);
            Vector3 dir = (tData.transform.position - MainCamera.transform.position).normalized;
            float ang = Vector3.Angle(MainCamera.transform.forward, dir);

            if (dist > data.MaxLockDistance || ang > 65f) // 65f gives a small buffer before losing lock
            {
                activeTargets.RemoveAt(i);
                continue;
            }

            // Update Timer
            tData.lockTimer += Time.deltaTime;
            if (tData.lockTimer >= data.LockOnTimeNeeded) tData.isLocked = true;

            // Update Prediction
            Rigidbody targetRb = tData.transform.GetComponent<Rigidbody>();
            Vector3 targetVel = (targetRb != null) ? targetRb.linearVelocity : Vector3.zero;
            float travelTime = dist / data.ProjectileSpeed;
            tData.predictedPos = tData.transform.position + (targetVel * travelTime);
        }

// 4. FIND PRIMARY TARGET (Updated)
        // We find the target closest to our 3D AIMING SPHERE (not the center of the screen)
        primaryTarget = null;
        float bestAngle = data.AssistConeAngle * 2; 

        // Calculate the direction from Camera to our Crosshair Sphere
        Vector3 aimDirection = (AimingObject.position - MainCamera.transform.position).normalized;

        foreach (var tData in activeTargets)
        {
            Vector3 dirToTarget = (tData.transform.position - MainCamera.transform.position).normalized;
            
            // Compare target direction against our Sphere direction
            float angleFromCrosshair = Vector3.Angle(aimDirection, dirToTarget);

            if (angleFromCrosshair < bestAngle)
            {
                bestAngle = angleFromCrosshair;
                primaryTarget = tData;
            }
        }
    }
    // --- DEBUG GIZMOS ---
    private void OnDrawGizmos()
    {
        if (activeTargets == null) return;

        foreach (var t in activeTargets)
        {
            if (t == null || t.transform == null) continue;

            // Yellow = Acquiring, Red = Locked, Green = Primary
            if (t == primaryTarget) Gizmos.color = Color.green;
            else if (t.isLocked) Gizmos.color = Color.red;
            else Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(t.predictedPos, 1f);
            Gizmos.DrawLine(t.transform.position, t.predictedPos);
        }
    }
}