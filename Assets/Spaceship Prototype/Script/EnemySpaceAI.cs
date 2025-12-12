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
    public float minAvoidanceTime = 3.5f; // Minimum time to spend avoiding

    [Header("References")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public Transform playerTarget;          // We will find this automatically

    // --- INTERNAL STATE ---
    private float currentSpeed;
    private float targetSpeed;
    private Quaternion targetRotation;
    private Camera mainCam; // Reference to the camera for the "On Screen" logic

    private float moveTimer;
    private float attackTimer;
    private float avoidanceDurationTimer; // Timer for how long to keep avoiding
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
        targetSpeed = Random.Range(minSpeed, maxSpeed);

        // --- NEW LOGIC: 50% Chance to fly into Camera View ---
        bool stayOnScreen = (Random.value > 0.5f);

        if (stayOnScreen && mainCam != null)
        {
            // 1. Pick a random point on the screen (0,0 is bottom-left, 1,1 is top-right)
            // We use 0.1 to 0.9 to keep them slightly away from the absolute edge
            Vector3 randomScreenPoint = new Vector3(Random.Range(0.1f, 0.9f), Random.Range(0.1f, 0.9f), Random.Range(20f, 50f)); // Z is distance from camera

            // 2. Convert that screen point to a world point
            Vector3 worldPoint = mainCam.ViewportToWorldPoint(randomScreenPoint);

            // 3. Look at that point
            targetRotation = Quaternion.LookRotation(worldPoint - transform.position);
        }
        else
        {
            // Old Logic (Standard Randomness)
            Vector3 randomDirection = Random.insideUnitSphere;
            randomDirection += transform.forward;
            targetRotation = Quaternion.LookRotation(randomDirection);
        }

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

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        bool isTooClose = distanceToPlayer <= avoidanceDistance;

        // 1. If we are too close, RESET the timer to full duration
        if (isTooClose)
        {
            avoidanceDurationTimer = minAvoidanceTime;
        }

        // 2. If the timer is still running, countdown
        if (avoidanceDurationTimer > 0)
        {
            avoidanceDurationTimer -= Time.deltaTime;
        }

        // 3. EXIT CONDITION: 
        // We only stop avoiding if we are far enough away AND the timer has reached 0
        if (!isTooClose && avoidanceDurationTimer <= 0)
        {
            return false;
        }

        // --- NEW LOGIC: Brake and Turn ---

        Vector3 directionAway = (transform.position - playerTarget.position).normalized;
        Quaternion lookAway = Quaternion.LookRotation(directionAway);

        // 1. Rotate towards the "Away" vector
        // Slerp ensures it's not a hard snap, but a curve
        transform.rotation = Quaternion.Slerp(transform.rotation, lookAway, Time.deltaTime * avoidanceTurnSpeed);

        // 2. Check how much we are facing away
        // angle == 0 means we are fully facing away. angle == 180 means we are facing the player.
        float angleToAvoidance = Vector3.Angle(transform.forward, directionAway);

        // 3. Dynamic Speed Control
        // If we are still facing the player (High Angle), SLOW DOWN to turn tighter.
        // If we are facing away (Low Angle), BOOST away.
        float speedMult = 1f;
        if (angleToAvoidance > 90)
        {
            // We are facing the player, brake to turn
            speedMult = 0.5f;
        }
        else
        {
            // We are facing away, punch it!
            speedMult = 1.5f;
        }

        // Apply the speed logic
        currentSpeed = Mathf.Lerp(currentSpeed, fleeSpeed * speedMult, Time.deltaTime * 2f);
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

        return true;
    }
}