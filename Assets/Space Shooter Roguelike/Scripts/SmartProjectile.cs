using UnityEngine;
using System.Collections.Generic;

// ============================================================================
// SMART PROJECTILE - ENHANCED
// ============================================================================

/// <summary>
/// Advanced projectile system supporting homing, piercing, ricochet, 
/// proximity detonation, explosions, and secondary payload spawning.
/// Fully integrated with WeaponData ScriptableObject configuration.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SmartProjectile : MonoBehaviour
{
    // ========================================================================
    // CORE STATE DATA
    // ========================================================================

    private DamagePayload baseDamagePayload;
    private Transform target;           // For homing missiles
    private float speed;
    private float turnSpeed;
    private bool isHoming;
    private GameObject sourceShip;      // The ship that fired this (for kill credit)

    // ========================================================================
    // ADVANCED BEHAVIORS
    // ========================================================================

    private int remainingPierces;       // How many enemies can we penetrate?
    private int remainingRicochets;     // How many bounces do we have left?
    private LayerMask ricochetLayers;   // What surfaces can we bounce off?
    private float explosionRadius;      // 0 = Single target hit
    private float explosionForce;       // Physics push power
    private float proximityRadius;      // 0 = Impact only, >0 = Proximity fuse

    // ========================================================================
    // CRITICAL HIT SYSTEM
    // ========================================================================

    private float critChance;           // Percentage chance to crit
    private float critMultiplier;       // Damage multiplier on crit

    // ========================================================================
    // SECONDARY PAYLOAD SYSTEM
    // ========================================================================

    private SpawnTrigger payloadTrigger;
    private float triggerValue;         // Distance/Time threshold
    private GameObject secondaryPrefab;
    private int spawnCount;
    private float spawnSpreadAngle;
    private bool inheritVelocity;
    private DamagePayload secondaryDamagePayload;

    // Tracking variables for payload triggers
    private float distanceTraveled = 0f;
    private float timeAlive = 0f;
    private Vector3 lastPosition;

    // ========================================================================
    // INTERNAL STATE
    // ========================================================================

    private Rigidbody rb;
    private LayerMask targetLayer;      // What triggers proximity/damage
    private bool hasExploded = false;   // Safety flag to prevent double explosions
    private bool hasSpawnedPayload = false; // Prevent multiple payload spawns
    private HashSet<Collider> piercedTargets = new HashSet<Collider>(); // Track pierced targets

    // ========================================================================
    // INITIALIZATION
    // ========================================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        // Critical for fast-moving projectiles to prevent clipping through objects
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        lastPosition = transform.position;
    }

    /// <summary>
    /// Initialize projectile with all configuration from WeaponData.
    /// Called by WeaponController immediately after spawning.
    /// </summary>
    public void Initialize(
        DamagePayload dmg,
        GameObject source,
        float projSpeed,
        float lifeTime,
        Transform homingTarget,
        float homingTurn,
        int pierceCount,
        int ricochetCount,
        LayerMask ricochetMask,
        float explodeRadius,
        float proxRadius,
        float explodeForce,
        float critChancePercent,
        float critDamageMultiplier,
        LayerMask enemyMask,
        SpawnTrigger payloadTrig,
        float triggerVal,
        GameObject secondaryPrefab,
        int spawnCnt,
        float spawnSpread,
        bool inheritVel,
        DamagePayload secondaryDmg)
    {
        // Core parameters
        baseDamagePayload = dmg;
        sourceShip = source;
        speed = projSpeed;
        target = homingTarget;
        turnSpeed = homingTurn;
        isHoming = (target != null);

        // Advanced behaviors
        remainingPierces = pierceCount;
        remainingRicochets = ricochetCount;
        ricochetLayers = ricochetMask;
        explosionRadius = explodeRadius;
        proximityRadius = proxRadius;
        explosionForce = explodeForce;
        targetLayer = enemyMask;

        // Critical hit system
        critChance = critChancePercent;
        critMultiplier = critDamageMultiplier;

        // Secondary payload system
        payloadTrigger = payloadTrig;
        triggerValue = triggerVal;
        this.secondaryPrefab = secondaryPrefab;
        spawnCount = spawnCnt;
        spawnSpreadAngle = spawnSpread;
        inheritVelocity = inheritVel;
        secondaryDamagePayload = secondaryDmg;

        // Ensure Source is set in payload for kill credit/vampirism
        baseDamagePayload.Source = sourceShip;
        secondaryDamagePayload.Source = sourceShip;

        // Auto-destroy after lifetime expires
        if (lifeTime > 0)
        {
            Destroy(gameObject, lifeTime);
        }
    }

    // ========================================================================
    // UPDATE LOOP
    // ========================================================================

    private void FixedUpdate()
    {
        if (hasExploded) return;

        // Track time and distance for payload triggers
        timeAlive += Time.fixedDeltaTime;
        distanceTraveled += Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;

        // --- MOVEMENT LOGIC ---
        UpdateMovement();

        // --- PROXIMITY DETECTION ---
        if (proximityRadius > 0)
        {
            CheckProximity();
        }

        // --- SECONDARY PAYLOAD CHECKS ---
        CheckPayloadTriggers();
    }

    // ========================================================================
    // MOVEMENT SYSTEM
    // ========================================================================

    private void UpdateMovement()
    {
        // Homing behavior: Gradually turn toward target
        if (isHoming && target != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);

            // Smoothly rotate toward target (limited by turn speed)
            rb.rotation = Quaternion.RotateTowards(
                rb.rotation,
                lookRotation,
                turnSpeed * Time.fixedDeltaTime
            );
        }

        // Always fly forward based on current facing direction
        rb.linearVelocity = transform.forward * speed;
    }

    // ========================================================================
    // PROXIMITY DETECTION
    // ========================================================================

    /// <summary>
    /// Checks if any enemies are within proximity radius and detonates if found.
    /// Used for flak cannons, proximity mines, etc.
    /// </summary>
    private void CheckProximity()
    {
        if (Physics.CheckSphere(transform.position, proximityRadius, targetLayer))
        {
            // Enemy detected! Ensure explosion radius covers proximity area
            if (explosionRadius < proximityRadius)
            {
                explosionRadius = proximityRadius;
            }

            Explode();
        }
    }

    // ========================================================================
    // SECONDARY PAYLOAD SYSTEM
    // ========================================================================

    /// <summary>
    /// Checks if conditions are met to spawn secondary payload.
    /// Supports: OnDistance, OnTimer triggers.
    /// </summary>
    private void CheckPayloadTriggers()
    {
        if (hasSpawnedPayload || payloadTrigger == SpawnTrigger.None) return;
        if (secondaryPrefab == null) return;

        bool shouldSpawn = false;

        switch (payloadTrigger)
        {
            case SpawnTrigger.OnDistance:
                if (distanceTraveled >= triggerValue)
                    shouldSpawn = true;
                break;

            case SpawnTrigger.OnTimer:
                if (timeAlive >= triggerValue)
                    shouldSpawn = true;
                break;
        }

        if (shouldSpawn)
        {
            SpawnSecondaryPayload();
        }
    }

    /// <summary>
    /// Spawns secondary projectiles/objects based on configuration.
    /// Examples: Cluster grenades, flak shrapnel, split missiles.
    /// </summary>
    private void SpawnSecondaryPayload()
    {
        hasSpawnedPayload = true;

        for (int i = 0; i < spawnCount; i++)
        {
            // Calculate spawn direction with spread
            Vector3 spawnDirection = transform.forward;

            if (spawnSpreadAngle > 0)
            {
                // Random spread within cone
                float angleX = Random.Range(-spawnSpreadAngle, spawnSpreadAngle) / 2f;
                float angleY = Random.Range(-spawnSpreadAngle, spawnSpreadAngle) / 2f;
                Quaternion spreadRotation = Quaternion.Euler(angleX, angleY, 0);
                spawnDirection = spreadRotation * spawnDirection;
            }

            // Instantiate secondary projectile
            GameObject spawned = Instantiate(
                secondaryPrefab,
                transform.position,
                Quaternion.LookRotation(spawnDirection)
            );

            // If spawned object is also a SmartProjectile, initialize it
            SmartProjectile spawnedProj = spawned.GetComponent<SmartProjectile>();
            if (spawnedProj != null)
            {
                float spawnSpeed = inheritVelocity ? speed : 20f; // Default speed for secondaries

                // Initialize with secondary payload settings (no further nesting)
                spawnedProj.Initialize(
                    secondaryDamagePayload,
                    sourceShip, // Maintain kill credit
                    spawnSpeed,
                    5f, // Default lifetime for secondary projectiles
                    null, // No homing for secondary
                    0f,
                    0, // No pierce
                    0, // No ricochet
                    ricochetLayers,
                    explosionRadius * 0.5f, // Smaller explosions
                    0f,
                    explosionForce * 0.5f,
                    0f, // No crit on secondary
                    1f,
                    targetLayer,
                    SpawnTrigger.None, // No tertiary payloads
                    0f,
                    null,
                    0,
                    0f,
                    false,
                    secondaryDamagePayload
                );
            }
            else
            {
                // If not a projectile, just apply velocity
                Rigidbody spawnedRb = spawned.GetComponent<Rigidbody>();
                if (spawnedRb != null && inheritVelocity)
                {
                    spawnedRb.linearVelocity = spawnDirection * speed;
                }
            }
        }

        // If payload trigger is OnDeath, we don't destroy here
        // If it's OnDistance/OnTimer, the projectile continues
    }

    // ========================================================================
    // COLLISION DETECTION
    // ========================================================================

    private void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;

        // Ignore player and other projectiles
        if (other.CompareTag("Player") || other.CompareTag("Projectile"))
            return;

        // Check for ricochet FIRST (before any other logic)
        if (remainingRicochets > 0 && IsRicochetSurface(other))
        {
            HandleRicochet(other);
            return; // Don't process damage/explosion on ricochet
        }

        // Handle payload spawning on impact
        if (payloadTrigger == SpawnTrigger.OnImpact && !hasSpawnedPayload)
        {
            SpawnSecondaryPayload();
        }

        // --- EXPLOSIVE PROJECTILE (Rocket/Grenade) ---
        if (explosionRadius > 0)
        {
            Explode();
            return;
        }

        // --- DIRECT HIT PROJECTILE (Bullet/Laser) ---
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            // Check if we've already pierced this target
            if (piercedTargets.Contains(other))
                return;

            // Calculate and apply damage with crit chance
            DamagePayload finalPayload = CalculateDamage(other.transform.position);
            damageable.TakeDamage(finalPayload);

            // Handle piercing
            if (remainingPierces > 0)
            {
                remainingPierces--;
                piercedTargets.Add(other); // Remember we hit this target

                // Optional: Reduce damage after each pierce (25% reduction)
                baseDamagePayload.PhysicalDamage *= 0.75f;
                baseDamagePayload.EnergyDamage *= 0.75f;
            }
            else
            {
                // No pierces left, destroy projectile
                HandleProjectileDeath();
            }
        }
        else
        {
            // Hit a wall/obstacle → Destroy
            HandleProjectileDeath();
        }
    }

    // ========================================================================
    // DAMAGE CALCULATION
    // ========================================================================

    /// <summary>
    /// Calculates final damage including critical hit chance and applies hit point.
    /// </summary>
    private DamagePayload CalculateDamage(Vector3 hitPosition)
    {
        DamagePayload finalPayload = baseDamagePayload;
        finalPayload.HitPoint = hitPosition;

        // Roll for critical hit
        bool isCrit = Random.Range(0f, 100f) < critChance;
        finalPayload.IsCritical = isCrit;

        if (isCrit)
        {
            finalPayload.PhysicalDamage *= critMultiplier;
            finalPayload.EnergyDamage *= critMultiplier;
        }

        return finalPayload;
    }

    // ========================================================================
    // RICOCHET SYSTEM
    // ========================================================================

    /// <summary>
    /// Checks if the collider is on a ricochet-eligible layer.
    /// </summary>
    private bool IsRicochetSurface(Collider collider)
    {
        return ((1 << collider.gameObject.layer) & ricochetLayers) != 0;
    }

    /// <summary>
    /// Handles ricochet bounce physics and direction change.
    /// </summary>
    private void HandleRicochet(Collider surface)
    {
        remainingRicochets--;

        // Calculate bounce direction
        Vector3 incomingDirection = rb.linearVelocity.normalized;
        Vector3 normal = Vector3.up; // Default fallback

        // Try to get accurate surface normal
        RaycastHit hit;
        if (Physics.Raycast(
            transform.position - incomingDirection * 0.1f,
            incomingDirection,
            out hit,
            0.2f,
            ricochetLayers))
        {
            normal = hit.normal;
        }

        // Reflect velocity
        Vector3 reflectedDirection = Vector3.Reflect(incomingDirection, normal);

        // Update rotation and velocity
        transform.rotation = Quaternion.LookRotation(reflectedDirection);
        rb.linearVelocity = reflectedDirection * speed;

        // Optional: Add slight randomness to bounce for variety
        // transform.rotation *= Quaternion.Euler(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 0);

        // Optional: Reduce speed after bounce (10% loss per bounce)
        // speed *= 0.9f;
    }

    // ========================================================================
    // EXPLOSION SYSTEM
    // ========================================================================

    /// <summary>
    /// Handles area-of-effect explosion damage and physics forces.
    /// </summary>
    private void Explode()
    {
        hasExploded = true;

        // TODO: Spawn explosion VFX here
        // Instantiate(ExplosionVFXPrefab, transform.position, Quaternion.identity);

        // Find all colliders in blast radius
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, targetLayer);

        foreach (Collider hit in hits)
        {
            // --- DAMAGE APPLICATION ---
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Calculate damage falloff based on distance from explosion center
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                float distanceRatio = distance / explosionRadius;

                // Inverse square falloff for more realistic explosion (closer = much more damage)
                float falloffMultiplier = 1f - (distanceRatio * distanceRatio);
                falloffMultiplier = Mathf.Max(falloffMultiplier, 0.2f); // Minimum 20% damage at edge

                // Create explosion damage payload
                DamagePayload explosionPayload = baseDamagePayload;
                explosionPayload.PhysicalDamage *= falloffMultiplier;
                explosionPayload.EnergyDamage *= falloffMultiplier;
                explosionPayload.HitPoint = hit.transform.position;

                // Roll for critical on each target in explosion
                bool isCrit = Random.Range(0f, 100f) < critChance;
                explosionPayload.IsCritical = isCrit;
                if (isCrit)
                {
                    explosionPayload.PhysicalDamage *= critMultiplier;
                    explosionPayload.EnergyDamage *= critMultiplier;
                }

                damageable.TakeDamage(explosionPayload);
            }

            // --- PHYSICS FORCE ---
            if (explosionForce > 0)
            {
                Rigidbody hitRb = hit.GetComponent<Rigidbody>();
                if (hitRb != null)
                {
                    hitRb.AddExplosionForce(
                        explosionForce,
                        transform.position,
                        explosionRadius,
                        1f, // Upward modifier
                        ForceMode.Impulse
                    );
                }
            }
        }

        // Destroy the projectile
        HandleProjectileDeath();
    }

    // ========================================================================
    // CLEANUP
    // ========================================================================

    /// <summary>
    /// Handles projectile destruction and OnDeath payload spawning.
    /// </summary>
    private void HandleProjectileDeath()
    {
        // Spawn payload on death if configured
        if (payloadTrigger == SpawnTrigger.OnDeath && !hasSpawnedPayload)
        {
            SpawnSecondaryPayload();
        }

        Destroy(gameObject);
    }

    // ========================================================================
    // EDITOR DEBUGGING
    // ========================================================================

    /// <summary>
    /// Visualizes proximity and explosion radii in Scene view.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Proximity radius (yellow)
        if (proximityRadius > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, proximityRadius);
        }

        // Explosion radius (red)
        if (explosionRadius > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }

        // Homing target indicator (cyan)
        if (isHoming && target != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}