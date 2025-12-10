using UnityEngine;
using UnityEngine.UI;

public class AutoTargetingSystem : MonoBehaviour
{
    [Header("References")]
    public Camera mainCam;
    public Transform firePoint; // Where bullets spawn
    public GameObject projectilePrefab; // The bullet prefab
    public LayerMask enemyLayer; // Only detect objects on this layer
    public RectTransform leadIndicatorUI;

    [Header("Targeting Settings")]
    public float detectionRange = 100f; // How far away can we see enemies?
    public float lockZoneRadius = 500f; // Radius in Pixels (The UI Circle size)
    public float projectileSpeed = 50f; // Set this to match your Bullet Script Speed!

    [Header("Lock-On Settings")]
    public float timeToLock = 1.0f; // How long to wait before firing (1 second)
    public Image crosshairImage;    // Optional: Reference to your UI Crosshair (to change color)
    public Color lockedColor = Color.green;
    public Color searchingColor = Color.red;

    // Private variables to track logic
    private float currentLockTimer = 0f;
    private Transform lastFrameTarget;

    [Header("Weapon Settings")]
    public float fireRate = 0.5f; // Seconds between shots


    private float nextFireTime = 0f;
    private Transform currentTarget;

    private Vector3 lastTargetPosition;
    private Vector3 targetVelocity;

    void Update()
    {
        // 1. Find who is currently in the crosshair (if anyone)
        Transform potentialTarget = GetTargetInCrosshair();

        // 2. Logic: Are we looking at the same person as last frame?
        if (potentialTarget != null)
        {
            currentTarget = potentialTarget;

            if (potentialTarget == lastFrameTarget)
            {
                // We are still looking at the same guy -> Increase Timer
                currentLockTimer += Time.deltaTime;
            }
            else
            {
                // We switched targets instantly -> Reset Timer
                currentLockTimer = 0f;
            }

            // 3. Visual Feedback (Optional)
            UpdateCrosshairColor(currentLockTimer >= timeToLock);

            // 4. Fire if locked
            if (currentLockTimer >= timeToLock)
            {
                currentTarget = potentialTarget; // Confirm the target
                HandleFiring();
            }
        }
        else
        {
            // Nothing in crosshair -> Reset everything
            currentLockTimer = 0f;
            currentTarget = null;
            UpdateCrosshairColor(false);
        }
        
        UpdateLeadIndicator();

        // Remember this target for the next frame check
        lastFrameTarget = potentialTarget;
    }

    Vector3 GetPredictedPosition()
    {
        // 1. Basic distance/time math (Same as before)
        float distanceToTarget = Vector3.Distance(firePoint.position, currentTarget.position);
        float timeToHit = distanceToTarget / projectileSpeed;

        // 2. --- THE FIX: Get Smoothed Velocity ---
        Vector3 currentVelocity = Vector3.zero;

        // Check if the enemy has our smoothing script
        VelocityTracker tracker = currentTarget.GetComponent<VelocityTracker>();

        if (tracker != null)
        {
            // YES: Use the averaged, smooth velocity
            currentVelocity = tracker.SmoothedVelocity;
        }
        else
        {
            // NO: Fallback to rigidBody (or however you were calculating 'targetVelocity' before)
            // Attempt to grab Rigidbody if tracker is missing
            Rigidbody rb = currentTarget.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Use the newer 'linearVelocity' API instead of the obsolete 'velocity'
                currentVelocity = rb.linearVelocity;
            }
        }

        // 3. Prediction Math (Use 'currentVelocity' instead of 'targetVelocity')
        return currentTarget.position + (currentVelocity * timeToHit);
    }

    void UpdateLeadIndicator()
    {
        if (leadIndicatorUI == null) return;

        // If we don't have a locked target, HIDE the UI and stop.
        if (currentTarget == null)
        {
            leadIndicatorUI.gameObject.SetActive(false);
            return;
        }

        Vector3 predictedPos = GetPredictedPosition();

        // Convert 3D world position to 2D screen position
        Vector3 screenPos = mainCam.WorldToScreenPoint(predictedPos);

        // Check if the predicted point is BEHIND the camera
        if (screenPos.z < 0)
        {
            leadIndicatorUI.gameObject.SetActive(false);
            return;
        }

        // Show and position the UI
        leadIndicatorUI.gameObject.SetActive(true);
        leadIndicatorUI.position = screenPos;
    }

    Transform GetTargetInCrosshair()
    {
        Transform bestTarget = null;
        float bestDistanceToCenter = Mathf.Infinity;

        Collider[] enemies = Physics.OverlapSphere(transform.position, detectionRange, enemyLayer);
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);

        foreach (Collider enemy in enemies)
        {
            Vector3 screenPos = mainCam.WorldToScreenPoint(enemy.transform.position);

            // Check if in front of camera
            if (screenPos.z > 0)
            {
                float distToCenter = Vector2.Distance(screenCenter, new Vector2(screenPos.x, screenPos.y));

                if (distToCenter < lockZoneRadius && distToCenter < bestDistanceToCenter)
                {
                    bestDistanceToCenter = distToCenter;
                    bestTarget = enemy.transform;
                }
            }
        }
        return bestTarget;
    }

    void UpdateCrosshairColor(bool isLocked)
    {
        if (crosshairImage != null)
        {
            // Lerp color: If locked -> Red, If searching -> Green
            crosshairImage.color = isLocked ? lockedColor : searchingColor;

            // OPTIONAL: Rotate the crosshair if locking on
            if (!isLocked && currentLockTimer > 0)
            {
                // Spin while locking!
                crosshairImage.rectTransform.Rotate(0, 0, -100 * Time.deltaTime);
            }
            else
            {
                // Reset rotation when done (or keep it spinning, your choice)
                crosshairImage.rectTransform.rotation = Quaternion.identity;
            }
        }
    }

    void HandleFiring()
    {
        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        Quaternion spawnRotation = firePoint.rotation;

        if (currentTarget != null)
        {
            // Use our helper function to get the spot to aim at
            Vector3 predictedPos = GetPredictedPosition();
            Vector3 directionToHit = (predictedPos - firePoint.position).normalized;
            spawnRotation = Quaternion.LookRotation(directionToHit);
        }

        GameObject bulletObj = Instantiate(projectilePrefab, firePoint.position, spawnRotation);

        SpaceProjectile bulletScript = bulletObj.GetComponent<SpaceProjectile>();
        if (bulletScript != null && bulletScript.isHoming)
        {
            bulletScript.SetTarget(currentTarget);
        }
    }

    // Draw the detection sphere in Editor to help you see range
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}