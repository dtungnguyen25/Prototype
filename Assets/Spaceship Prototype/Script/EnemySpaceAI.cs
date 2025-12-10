using UnityEngine;

public class EnemySpaceAI : MonoBehaviour
{
    // --- SETTINGS ---
    [Header("Movement Settings")]
    public float minSpeed = 10f;
    public float maxSpeed = 30f;
    public float rotationSpeed = 10f;       // How fast it wanders
    public float changeDirectionInterval = 3f; // How often to pick a new random direction

    [Header("Combat Settings")]
    public float attackIntervalMin = 5f;
    public float attackIntervalMax = 10f;
    public float combatTurnSpeed = 5f;      // How fast it turns to aim at you
    public float shootingAccuracy = 10f;    // In degrees (if angle < 10, shoot)

    [Header("Avoidance Settings")]
    public float avoidanceDistance = 30f; // If closer than this, fly away
    public float avoidanceTurnSpeed = 5f; // Turn away quickly
    public float fleeSpeed = 25f;         // Speed while running away

    [Header("References")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public Transform playerTarget;          // We will find this automatically

    // --- INTERNAL STATE ---
    private float currentSpeed;
    private float targetSpeed;
    private Quaternion targetRotation;

    private float moveTimer;
    private float attackTimer;
    private bool isAttacking = false;

    void Start()
    {
        // 1. Find the player automatically (Make sure your Player has the tag "Player")
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;

        // 2. Initialize random timers
        ResetAttackTimer();
        PickNewMovementValues();
    }

    void Update()
    {
        if (playerTarget == null)
        {
            Wander();
            return;
        }

        // --- PRIORITY 1: AVOIDANCE ---
        // If HandleAvoidance returns true, it means we are too close.
        // We skip the rest of the code (Attack/Wander) for this frame.
        if (HandleAvoidance())
        {
            return;
        }

        // --- PRIORITY 2: ATTACK / WANDER ---
        if (isAttacking)
        {
            HandleAttack();
        }
        else
        {
            Wander();

            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0)
            {
                isAttacking = true;
            }
        }
    }

    // --- BEHAVIOR 1: WANDER ---
    void Wander()
    {
        // 1. Move Forward constantly
        // Smoothly interpolate current speed to the random target speed
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime);
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

        // 2. Rotate Smoothly
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed * 0.1f);

        // 3. Pick new random values periodically
        moveTimer -= Time.deltaTime;
        if (moveTimer <= 0)
        {
            PickNewMovementValues();
        }
    }

    void PickNewMovementValues()
    {
        // Randomize Speed
        targetSpeed = Random.Range(minSpeed, maxSpeed);

        // Randomize Direction (Point somewhere random inside a sphere)
        Vector3 randomDirection = Random.insideUnitSphere;
        // Keep moving generally forward-ish so they don't loop in tight circles constantly
        randomDirection += transform.forward;

        targetRotation = Quaternion.LookRotation(randomDirection);

        // Reset move timer
        moveTimer = changeDirectionInterval;
    }

    // --- BEHAVIOR 2: ATTACK ---
    void HandleAttack()
    {
        // 1. Slow down a bit while aiming
        currentSpeed = Mathf.Lerp(currentSpeed, minSpeed / 2, Time.deltaTime);
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

        // 2. Turn to face the Player
        Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
        Quaternion lookAtPlayer = Quaternion.LookRotation(directionToPlayer);

        // Slerp towards player (Combat Turn Speed is usually faster than wander turn speed)
        transform.rotation = Quaternion.Slerp(transform.rotation, lookAtPlayer, Time.deltaTime * combatTurnSpeed);

        // 3. Check angle to decide if we can shoot
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        if (angleToPlayer < shootingAccuracy)
        {
            Fire();
            // Stop attacking and go back to wandering
            isAttacking = false;
            ResetAttackTimer();
            PickNewMovementValues(); // Pick a new wander path immediately after shooting
        }
    }

    void Fire()
    {
        if (projectilePrefab && firePoint)
        {
            // Instantiate the bullet you already made
            Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        }
    }

    void ResetAttackTimer()
    {
        attackTimer = Random.Range(attackIntervalMin, attackIntervalMax);
    }

    bool HandleAvoidance()
    {
        if (playerTarget == null) return false;

        // 1. Check distance
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // 2. If we are safe, return 'false' so the normal AI can run
        if (distanceToPlayer > avoidanceDistance)
        {
            return false;
        }

        // --- AVOIDANCE LOGIC ---

        // 3. Calculate vector AWAY from player (Me - Player = Direction away)
        Vector3 directionAway = (transform.position - playerTarget.position).normalized;

        // 4. Create rotation looking away
        Quaternion lookAway = Quaternion.LookRotation(directionAway);

        // 5. Smoothly rotate to that direction
        transform.rotation = Quaternion.Slerp(transform.rotation, lookAway, Time.deltaTime * avoidanceTurnSpeed);

        // 6. Move forward (usually at a brisk pace)
        // We blend current speed to fleeSpeed for smoothness
        currentSpeed = Mathf.Lerp(currentSpeed, fleeSpeed, Time.deltaTime);
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

        // Return 'true' to tell the Update loop "I am busy avoiding, don't do anything else"
        return true;
    }
}