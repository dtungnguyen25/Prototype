using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SmartProjectile : MonoBehaviour
{
    // --- STATE DATA ---
    private DamagePayload payload;
    private Transform target;       // For Homing
    private float speed;
    private float turnSpeed;
    private bool isHoming;

    // --- ADVANCED BEHAVIORS ---
    private int remainingPierces;   // How many enemies can we go through?
    private float explosionRadius;  // 0 = Single Target
    private float explosionForce;   // Push power
    private float proximityRadius;  // 0 = Impact only
    
    // --- INTERNALS ---
    private Rigidbody rb;
    private LayerMask targetLayer;  // To know what triggers proximity
    private bool hasExploded = false; // Safety flag to prevent double explosions

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; 
        // Important for fast moving objects to not clip through walls
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; 
    }

    /// <summary>
    /// Called by WeaponController to setup the bullet immediately after spawning.
    /// </summary>
    public void Initialize(DamagePayload dmg, float projSpeed, float lifeTime, 
                           Transform homingTarget, float homingTurn, 
                           int pierceCount, float explodeRadius, float proxRadius, float explodeForce,
                           LayerMask enemyMask)
    {
        // 1. Store Data
        payload = dmg;
        speed = projSpeed;
        target = homingTarget;
        turnSpeed = homingTurn;
        isHoming = (target != null);

        remainingPierces = pierceCount;
        explosionRadius = explodeRadius;
        proximityRadius = proxRadius;
        explosionForce = explodeForce;
        targetLayer = enemyMask;

        // 2. Set Lifetime (Auto-destroy if it hits nothing)
        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        if (hasExploded) return;

        // --- 1. MOVEMENT LOGIC ---
        if (isHoming && target != null)
        {
            // Calculate direction to target
            Vector3 direction = (target.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            
            // Smoothly rotate towards the target (Limited by Turn Speed)
            rb.rotation = Quaternion.RotateTowards(rb.rotation, lookRotation, turnSpeed * Time.fixedDeltaTime);
        }

        // Always fly "Forward" based on where we are facing
        rb.linearVelocity = transform.forward * speed;

        // --- 2. PROXIMITY LOGIC ---
        // If this is a "Proximity Fuse" weapon (like Flak Cannon)
        if (proximityRadius > 0)
        {
            CheckProximity();
        }
    }

    // Checks if an enemy is close enough to detonate
    private void CheckProximity()
    {
        // We use OverlapSphere to "feel" for enemies
        if (Physics.CheckSphere(transform.position, proximityRadius, targetLayer))
        {
            // We found an enemy! Detonate.
            // We set explosion radius to at least the proximity radius so we actually hit them.
            if (explosionRadius < proximityRadius) explosionRadius = proximityRadius;
            
            Explode();
        }
    }

    // --- 3. IMPACT LOGIC ---
    private void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;

        // Ignore Self/Player/Other Bullets
        if (other.CompareTag("Player") || other.CompareTag("Projectile")) return;

        // CASE A: EXPLOSIVE (Rocket/Grenade)
        if (explosionRadius > 0)
        {
            Explode();
            return;
        }

        // CASE B: DIRECT HIT (Laser/Bullet)
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            // 1. Deliver Damage
            payload.HitPoint = transform.position; // Update location for VFX
            damageable.TakeDamage(payload);

            // 2. Handle Piercing
            if (remainingPierces > 0)
            {
                remainingPierces--;
                // Optional: Reduce damage after piercing?
                // payload.PhysicalDamage *= 0.8f; 
            }
            else
            {
                // No pierces left, destroy bullet
                Destroy(gameObject);
            }
        }
        else
        {
            // Hit a wall/obstacle -> Destroy
            Destroy(gameObject);
        }
    }

    // --- 4. EXPLOSION LOGIC ---
    private void Explode()
    {
        hasExploded = true;
        
        // 1. Visuals: Spawn your explosion particle prefab here
        // Instantiate(ExplosionVFX, transform.position, Quaternion.identity);

        // 2. Find everyone in the blast zone
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, targetLayer);

        foreach (Collider hit in hits)
        {
            // A. Deal Damage
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Note: You can add "Falloff" logic here (less damage at edge of radius)
                damageable.TakeDamage(payload);
            }

            // B. Apply Physics Force
            if (explosionForce > 0)
            {
                Rigidbody hitRb = hit.GetComponent<Rigidbody>();
                if (hitRb != null)
                {
                    hitRb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
                }
            }
        }

        // 3. Destroy the missile
        Destroy(gameObject);
    }

    // Editor Debugging: Draw the proximity/explosion spheres
    private void OnDrawGizmosSelected()
    {
        if (proximityRadius > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, proximityRadius);
        }
        if (explosionRadius > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}