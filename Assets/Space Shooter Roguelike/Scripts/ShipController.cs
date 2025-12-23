using UnityEngine;
using UnityEngine.InputSystem;

// ============================================================================
// SHIP CONTROLLER
// ============================================================================

/// <summary>
/// Advanced ship controller supporting 6DOF movement, boost, dodge, auto-stabilization,
/// and weapon systems. Uses Unity's New Input System.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    // ========================================================================
    // DEPENDENCIES
    // ========================================================================

    [Header("Dependencies")]
    [Tooltip("The camera the ship moves relative to. Used for camera-space controls.")]
    public Transform cameraTransform;

    [Header("Configuration")]
    [Tooltip("Ship stats ScriptableObject (e.g., LightShip, HeavyShip).")]
    public ShipStats currentShipStats;

    [Header("Weapons")]
    [Tooltip("Primary weapon controller (RT trigger).")]
    public WeaponController primaryWeapon;

    [Tooltip("Secondary weapon controller (RB button).")]
    public WeaponController secondaryWeapon;

    // ========================================================================
    // RUNTIME STATS (MODIFIABLE)
    // ========================================================================

    // These are copied from ShipStats and can be modified at runtime by buffs/debuffs
    private float currentAcceleration;
    private float currentMaxSpeed;
    private float currentMaxBoostSpeed;
    private float currentBoostForce;
    private float currentBrakeStrength;
    private float currentTurnSpeed;
    private float currentAutoLevelSpeed;
    private float currentAutoLevelDelay;
    private float currentMaxSpeedForAutoLevel;
    private float currentDodgeForce;
    private float currentEnergyPerDodge;
    private float currentMaxEnergy;
    private float currentEnergyDrainRate;
    private float currentEnergyRechargeRate;
    private float currentEnergyRechargeDelay;

    // ========================================================================
    // INTERNAL STATE
    // ========================================================================

    private bool isBoosting = false;
    private bool isBrakingEnabled = true;
    private float currentEnergy;
    private float lastEnergyUseTime;
    private float lastInputTime;

    // ========================================================================
    // INPUT STORAGE
    // ========================================================================

    private Vector2 moveInput;           // WASD / Left Stick
    private Vector2 lookInput;           // Mouse / Right Stick
    private float thrustForwardInput;    // RT trigger
    private float thrustBackwardInput;   // LB button

    // ========================================================================
    // COMPONENT REFERENCES
    // ========================================================================

    private Rigidbody rb;

    // ========================================================================
    // INITIALIZATION
    // ========================================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Configure rigidbody for space flight
        rb.useGravity = false;
        rb.linearDamping = 1f;
        rb.angularDamping = 2f;

        // Load ship stats
        if (currentShipStats != null)
        {
            LoadShipStats(currentShipStats);
        }
        else
        {
            Debug.LogWarning("No ShipStats assigned to ShipController!");
        }

        currentEnergy = currentMaxEnergy;
    }

    /// <summary>
    /// Loads stats from a ShipStats ScriptableObject into runtime variables.
    /// Call this to switch ships or reset stats.
    /// </summary>
    public void LoadShipStats(ShipStats stats)
    {
        // Movement
        currentAcceleration = stats.acceleration;
        currentMaxSpeed = stats.maxSpeed;
        currentMaxBoostSpeed = stats.maxBoostSpeed;
        currentBoostForce = stats.boostForce;
        currentBrakeStrength = stats.brakeStrength;

        // Rotation
        currentTurnSpeed = stats.turnSpeed;
        currentAutoLevelSpeed = stats.autoLevelSpeed;
        currentAutoLevelDelay = stats.autoLevelDelay;
        currentMaxSpeedForAutoLevel = stats.maxSpeedForAutoLevel;

        // Abilities
        currentDodgeForce = stats.dodgeForce;
        currentEnergyPerDodge = stats.energyPerDodge;
        currentMaxEnergy = stats.maxEnergy;
        currentEnergyDrainRate = stats.energyDrainRate;
        currentEnergyRechargeRate = stats.energyRechargeRate;
        currentEnergyRechargeDelay = stats.energyRechargeDelay;
    }

    // ========================================================================
    // UPDATE LOOPS
    // ========================================================================

    private void Update()
    {
        HandleEnergy();
    }

    private void FixedUpdate()
    {
        HandleMovement();
        HandleRotation();

        if (isBrakingEnabled)
        {
            HandleStabilization();
            HandleBraking();
        }
    }

    // ========================================================================
    // INPUT SYSTEM CALLBACKS
    // ========================================================================

    /// <summary>
    /// Strafe input (WASD / Left Stick).
    /// </summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// Rotation input (Mouse / Right Stick).
    /// </summary>
    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// Forward thrust input (RT trigger).
    /// </summary>
    public void OnThrustForward(InputAction.CallbackContext context)
    {
        thrustForwardInput = context.ReadValue<float>();
    }

    /// <summary>
    /// Backward thrust input (LB button).
    /// </summary>
    public void OnThrustBackward(InputAction.CallbackContext context)
    {
        thrustBackwardInput = context.ReadValue<float>();
    }

    /// <summary>
    /// Boost input (A button / Space).
    /// </summary>
    public void OnBoost(InputAction.CallbackContext context)
    {
        if (context.started) isBoosting = true;
        if (context.canceled) isBoosting = false;
    }

    /// <summary>
    /// Dodge input (B button / Shift).
    /// </summary>
    public void OnDodge(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            PerformDodge();
        }
    }

    /// <summary>
    /// Primary weapon fire (RT trigger).
    /// </summary>
    public void OnFirePrimary(InputAction.CallbackContext context)
    {
        if (primaryWeapon == null) return;

        if (context.performed)
        {
            primaryWeapon.StartFiring();
        }
        if (context.canceled)
        {
            primaryWeapon.StopFiring();
        }
    }

    /// <summary>
    /// Secondary weapon fire (RB button).
    /// </summary>
    public void OnFireSecondary(InputAction.CallbackContext context)
    {
        if (secondaryWeapon == null) return;

        if (context.started)
        {
            secondaryWeapon.StartFiring();
        }
        if (context.canceled)
        {
            secondaryWeapon.StopFiring();
        }
    }

    // ========================================================================
    // ENERGY SYSTEM
    // ========================================================================

    private void HandleEnergy()
    {
        if (isBoosting && currentEnergy > 0)
        {
            // Drain energy while boosting
            currentEnergy -= currentEnergyDrainRate * Time.deltaTime;
            lastEnergyUseTime = Time.time;

            // Clamp to prevent negative
            if (currentEnergy < 0)
            {
                currentEnergy = 0;
                isBoosting = false;
            }
        }
        else
        {
            // Stop boosting if out of energy
            if (currentEnergy <= 0)
            {
                isBoosting = false;
            }

            // Recharge after delay
            if (Time.time > lastEnergyUseTime + currentEnergyRechargeDelay)
            {
                currentEnergy += currentEnergyRechargeRate * Time.deltaTime;
                currentEnergy = Mathf.Min(currentEnergy, currentMaxEnergy);
            }
        }
    }

    // ========================================================================
    // MOVEMENT SYSTEM
    // ========================================================================

    private void HandleMovement()
    {
        // 1. Calculate thrust from forward/backward input
        float thrustAxis = thrustForwardInput - thrustBackwardInput;
        Vector3 inputThrust = transform.forward * thrustAxis * currentAcceleration;

        // 2. Calculate strafe force (camera-relative)
        Vector3 camUp = cameraTransform.up;
        Vector3 camRight = cameraTransform.right;
        Vector3 strafeForce = (camUp * moveInput.y + camRight * moveInput.x) * currentAcceleration;

        // 3. Apply input forces
        rb.AddForce(inputThrust + strafeForce);

        // 4. Handle boost
        float speedLimit = currentMaxSpeed;

        if (isBoosting)
        {
            speedLimit = currentMaxBoostSpeed;

            // Apply constant forward boost force
            Vector3 boostForce = transform.forward * currentBoostForce;
            rb.AddForce(boostForce);
        }

        // 5. Soft speed cap
        if (rb.linearVelocity.magnitude > speedLimit)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * speedLimit;
        }
    }

    // ========================================================================
    // ROTATION SYSTEM
    // ========================================================================

    private void HandleRotation()
    {
        // Pitch (up/down) and Yaw (left/right)
        Vector3 torque = new Vector3(-lookInput.y, lookInput.x, 0) * currentTurnSpeed;
        rb.AddRelativeTorque(torque);
    }

    // ========================================================================
    // STABILIZATION SYSTEM (CAMERA-RELATIVE ROLL)
    // ========================================================================

    /// <summary>
    /// Auto-stabilizes ship roll to align with camera's up direction.
    /// This keeps the ship "level" relative to the screen, not world up.
    /// </summary>
    private void HandleStabilization()
    {
        // 1. Check if player is actively rotating
        bool isRotating = lookInput.sqrMagnitude > 0.01f;

        // 2. Check if moving too fast for stabilization
        float currentSpeed = rb.linearVelocity.magnitude;
        bool isMovingFast = currentSpeed > currentMaxSpeedForAutoLevel;

        if (isRotating || isMovingFast)
        {
            // Reset timer while player has control
            lastInputTime = Time.time;
            return;
        }

        // 3. Check if delay has passed
        float timeSinceInput = Time.time - lastInputTime;
        if (timeSinceInput < currentAutoLevelDelay)
        {
            return;
        }

        // 4. Calculate roll correction to match camera up
        // We want ship's up to align with camera's up (screen Y axis)
        Vector3 shipUp = transform.up;
        Vector3 cameraUp = cameraTransform.up;

        // Project both vectors onto the plane perpendicular to ship's forward
        Vector3 shipForward = transform.forward;
        Vector3 projectedShipUp = Vector3.ProjectOnPlane(shipUp, shipForward).normalized;
        Vector3 projectedCameraUp = Vector3.ProjectOnPlane(cameraUp, shipForward).normalized;

        // Calculate the rotation needed to align projected up vectors
        Quaternion targetRotation = Quaternion.FromToRotation(projectedShipUp, projectedCameraUp);
        targetRotation.ToAngleAxis(out float angle, out Vector3 axis);

        // Normalize angle to -180 to 180 range
        if (angle > 180f) angle -= 360f;

        // Apply torque only around the forward axis (roll only)
        // We project the axis onto the forward direction to isolate roll
        float rollComponent = Vector3.Dot(axis, shipForward);
        Vector3 rollAxis = shipForward * rollComponent;

        // Apply stabilization torque
        Vector3 stabilizationTorque = rollAxis * (angle * currentAutoLevelSpeed * Mathf.Deg2Rad);
        rb.AddTorque(stabilizationTorque - rb.angularVelocity * 0.5f); // Add damping
    }

    // ========================================================================
    // BRAKING SYSTEM
    // ========================================================================

    private void HandleBraking()
    {
        // Check if no input is detected
        bool noInput = moveInput.sqrMagnitude < 0.1f &&
                      Mathf.Abs(thrustForwardInput) < 0.1f &&
                      Mathf.Abs(thrustBackwardInput) < 0.1f;

        if (noInput)
        {
            // Brake linear velocity
            rb.linearVelocity = Vector3.Lerp(
                rb.linearVelocity,
                Vector3.zero,
                currentBrakeStrength * Time.fixedDeltaTime
            );

            // Brake angular velocity if not rotating
            if (lookInput.sqrMagnitude < 0.1f)
            {
                rb.angularVelocity = Vector3.Lerp(
                    rb.angularVelocity,
                    Vector3.zero,
                    currentBrakeStrength * Time.fixedDeltaTime
                );
            }
        }
    }

    // ========================================================================
    // DODGE SYSTEM
    // ========================================================================

    private void PerformDodge()
    {
        // 1. Check energy cost
        if (currentEnergy < currentEnergyPerDodge)
        {
            // TODO: Play failure sound/effect
            return;
        }

        // 2. Consume energy
        currentEnergy -= currentEnergyPerDodge;
        lastEnergyUseTime = Time.time;

        // 3. Calculate dodge direction
        Vector3 dodgeDirection;

        if (moveInput.sqrMagnitude > 0.1f)
        {
            // Directional dodge based on input
            Vector3 camUp = cameraTransform.up;
            Vector3 camRight = cameraTransform.right;
            dodgeDirection = (camUp * moveInput.y + camRight * moveInput.x).normalized;
        }
        else
        {
            // Default forward dodge if no input
            dodgeDirection = transform.forward;
        }

        // 4. Apply impulse force
        rb.AddForce(dodgeDirection * currentDodgeForce, ForceMode.Impulse);

        // TODO: Trigger dodge VFX/sound
        // TODO: Add invincibility frames if desired
    }

    // ========================================================================
    // PUBLIC UTILITY METHODS
    // ========================================================================

    /// <summary>
    /// Applies an explosion force to the ship. Call from explosion scripts.
    /// </summary>
    public void ApplyExplosion(Vector3 position, float force, float radius)
    {
        rb.AddExplosionForce(force, position, radius);
    }

    /// <summary>
    /// Enables or disables braking system (useful for damage states).
    /// </summary>
    public void SetBrakingEnabled(bool enabled)
    {
        isBrakingEnabled = enabled;
    }

    /// <summary>
    /// Gets current energy as a percentage (0-1).
    /// </summary>
    public float GetEnergyPercent()
    {
        return currentMaxEnergy > 0 ? currentEnergy / currentMaxEnergy : 0f;
    }

    /// <summary>
    /// Gets current energy value.
    /// </summary>
    public float GetCurrentEnergy()
    {
        return currentEnergy;
    }

    /// <summary>
    /// Checks if ship is currently boosting.
    /// </summary>
    public bool IsBoosting()
    {
        return isBoosting;
    }

    /// <summary>
    /// Gets current speed as a percentage of max speed (0-1).
    /// </summary>
    public float GetSpeedPercent()
    {
        float currentSpeed = rb.linearVelocity.magnitude;
        float maxPossibleSpeed = isBoosting ? currentMaxBoostSpeed : currentMaxSpeed;
        return maxPossibleSpeed > 0 ? currentSpeed / maxPossibleSpeed : 0f;
    }

    /// <summary>
    /// Applies a stat modifier (buff/debuff). Example: speed boost power-up.
    /// </summary>
    public void ApplySpeedModifier(float multiplier)
    {
        currentMaxSpeed *= multiplier;
        currentMaxBoostSpeed *= multiplier;
    }

    /// <summary>
    /// Restores energy to full (e.g., from pickup).
    /// </summary>
    public void RestoreEnergy(float amount)
    {
        currentEnergy = Mathf.Min(currentEnergy + amount, currentMaxEnergy);
    }
}