using NUnit.Framework.Internal.Commands;
using UnityEngine;
using UnityEngine.InputSystem; // Required for the New Input System

[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("The camera the ship moves relative to.")]
    public Transform cameraTransform;

    [Header("Configuration")]
    // This is where you drag in your "LightShip", "HeavyShip", etc.
    public ShipStats currentShipStats; 

    // --- RUNTIME VARIABLES (Hidden from Inspector) ---
    // We use these for actual physics. This prepares you for modifiers.
    // e.g., currentAcceleration can be changed by a buff without breaking the original asset.
    private float currentAcceleration;
    private float currentMaxSpeed;
    private float currentBoostSpeed;
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
    
    // Internal State Flags
    private bool isBoosting = false;
    private bool isBrakingEnabled = true; // Can be disabled later for damage effects
    private float currentEnergy;
    private float lastEnergyUseTime; // Timestamp of last energy use

    // Input Storage
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float thrustForwardInput;
    private float thrustBackwardInput;
    private float lastInputTime; // For auto-leveling

    // Component References
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Better for space games: turn off gravity so we don't fall
        rb.useGravity = false;
        // Add some drag so the ship doesn't float forever after an explosion
        rb.linearDamping = 1f; 
        rb.angularDamping = 2f;

        // Initialize the ship with the stats assigned in the inspector
        if (currentShipStats != null)
        {
            LoadShipStats(currentShipStats);
        }

        currentEnergy = currentMaxEnergy;

    }
    /// <summary>
    /// Call this method to switch ships or reset stats.
    /// It copies values from the Asset (Read-Only) to the Controller (Modifiable).
    /// </summary>
    public void LoadShipStats(ShipStats stats)
    {

        // 1. Copy Movement values to our Runtime variables
        currentAcceleration = stats.acceleration;
        currentMaxSpeed = stats.maxSpeed;
        currentBoostSpeed = stats.boostSpeed;
        currentBrakeStrength = stats.brakeStrength;

        // 2. Copy Rotation values
        currentTurnSpeed = stats.turnSpeed;
        currentAutoLevelSpeed = stats.autoLevelSpeed;
        currentAutoLevelDelay = stats.autoLevelDelay;
        currentMaxSpeedForAutoLevel = stats.maxSpeedForAutoLevel;

        // 3. Copy Ability values
        currentDodgeForce = stats.dodgeForce;
        currentEnergyPerDodge = stats.energyPerDodge;
        currentMaxEnergy = stats.maxEnergy;
        currentEnergyDrainRate = stats.energyDrainRate;
        currentEnergyRechargeRate = stats.energyRechargeRate;
        currentEnergyRechargeDelay = stats.energyRechargeDelay;

    }

    #region Input Handling
    // Link these methods to your PlayerInput component events in the Inspector
    // OR use the generated C# class. For simplicity, here are public methods compatible with PlayerInput "Invoke Unity Events".

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnThrustForward(InputAction.CallbackContext context)
    {
        thrustForwardInput = context.ReadValue<float>();
    }

    public void OnThrustBackward(InputAction.CallbackContext context) // For LB
    {
        // LB is a button, so it returns 0 or 1
        thrustBackwardInput = context.ReadValue<float>();
    }

    public void OnBoost(InputAction.CallbackContext context)
    {
        // Check if button is pressed or released
        if (context.started) isBoosting = true;
        if (context.canceled) isBoosting = false;
    }

    public void OnDodge(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            PerformDodge();
        }
    }
    #endregion

    private void Update()
    {
        // Handle Energy Logic in Update (Frame-based)
        HandleEnergy();
        Debug.Log("Current Energy: " + currentEnergy.ToString("F1") );
    }

    private void FixedUpdate()
    {
        // Handle Physics Logic in FixedUpdate (Physics-tick based)
        HandleMovement();
        HandleRotation();
        
        if (isBrakingEnabled)
        {
            HandleStabilization();
            HandleBraking();
        }
    }

    #region Logic Implementation

    private void HandleEnergy()
    {
        if (isBoosting && currentEnergy > 0)
        {
            currentEnergy -= currentEnergyDrainRate * Time.deltaTime; // Drain energy
            lastEnergyUseTime = Time.time; // Update last use time
        }
        else
        {
            // Stop boosting if out of energy
            if (currentEnergy <= 0) isBoosting = false;
            
            // CHECK: Has enough time passed since we last used energy?
            if (Time.time > lastEnergyUseTime + currentEnergyRechargeDelay)
            {
            // Recharge
            currentEnergy = Mathf.Clamp(currentEnergy + currentEnergyRechargeRate * Time.deltaTime, 0, currentMaxEnergy);
            }
        }
    }

    private void HandleMovement()
    {
        // 1. Calculate Thrust (LT - LB)
        float thrustAxis = thrustForwardInput - thrustBackwardInput;
        Vector3 thrustForce = transform.forward * thrustAxis * currentAcceleration;

        // 2. Calculate Strafe (Left Stick relative to Camera)
        // We project the camera's up and right vectors onto a flat plane so looking down doesn't mess up movement
        Vector3 camUp = cameraTransform.up;
        Vector3 camRight = cameraTransform.right;

        Vector3 strafeForce = (camUp * moveInput.y + camRight * moveInput.x) * currentAcceleration;

        // 3. Apply Forces
        float speedLimit = isBoosting ? currentBoostSpeed : currentMaxSpeed;
        
        // If boosting, we multiply the force
        float boostMultiplier = isBoosting ? 2f : 1f;

        rb.AddForce((thrustForce + strafeForce) * boostMultiplier);

        // 4. Cap Speed (Soft cap)
        if (rb.linearVelocity.magnitude > speedLimit)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * speedLimit;
        }
    }

    private void HandleRotation()
    {
        // Simple turning using torque
        // Pitch (Up/Down) rotates around X axis
        // Yaw (Left/Right) rotates around Y axis
        Vector3 torque = new Vector3(lookInput.y * -1f, lookInput.x, 0) * currentTurnSpeed; // invert Y here if needed
        
        // Use AddRelativeTorque so it rotates based on where the ship is currently facing
        rb.AddRelativeTorque(torque);
    }

private void HandleStabilization()
{
    // 1. Check if we have any rotation input (Using the input variable we set up earlier)
    // We check .sqrMagnitude because it's faster than .magnitude
    bool isRotating = lookInput.sqrMagnitude > 0.01f;

    // 2. Check Speed
    float currentSpeed = rb.linearVelocity.magnitude;
    bool isMovingFast = currentSpeed > currentMaxSpeedForAutoLevel;

    if (isRotating || isMovingFast)
    {
        // If we are rotating, reset the "Last Input Time" to right now
        lastInputTime = Time.time;
    }
    else
    {
        // 2. We are NOT rotating. Check how much time has passed.
        float timeSinceInput = Time.time - lastInputTime;

        if (timeSinceInput > currentAutoLevelDelay)
        {
            // 3. The Delay is over! Apply Stabilization.
            
            // Calculate the rotation needed to align 'up' with 'Vector3.up'
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, Vector3.up);
            
            targetRotation.ToAngleAxis(out float angle, out Vector3 axis);

            // Normalize angle to handle the -180 to 180 wrap-around
            if (angle > 180) angle -= 360;

            // Apply the Torque (Physics force) to gently rotate us back
            // We use the same 'stabilizeSpeed' variable from your stats
            rb.AddTorque(axis * (angle * currentAutoLevelSpeed * Mathf.Deg2Rad) - rb.angularVelocity);
        }
    }
}

     private void HandleBraking()
    {
        // If no input is detected, apply counter-force to stop sliding
        bool noInput = moveInput.sqrMagnitude < 0.1f && Mathf.Abs(thrustForwardInput) < 0.1f && Mathf.Abs(thrustBackwardInput) < 0.1f;

        if (noInput)
        {
            // Interpolate velocity towards zero
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, currentBrakeStrength * Time.fixedDeltaTime);
            
            // Also kill rotation if joystick is let go
            if(lookInput.sqrMagnitude < 0.1f)
            {
                rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, currentBrakeStrength * Time.fixedDeltaTime);
            }
        }
    }

    private void PerformDodge()
    {
        // 1. Check if we have enough energy
    if (currentEnergy < currentEnergyPerDodge)
    {
        // To be doing: Play a "failed" sound or flash UI here
        return; 
    }

        // 2. Pay the cost (Instant deduction)
        currentEnergy -= currentEnergyPerDodge;
        lastEnergyUseTime = Time.time; // Update last use time
        
        // "B: Dodge, move fast toward curent input direction"
        Vector3 dodgeDirection;

        if (moveInput.sqrMagnitude > 0.1f)
        {
            // Directional Dodge: Use the same math as Strafe logic
            Vector3 camUp = cameraTransform.up;
            Vector3 camRight = cameraTransform.right;
            dodgeDirection = (camUp * moveInput.y + camRight * moveInput.x).normalized;
        }
        else
        {
            // "Phase/Spot Dodge": If no input, maybe jump UP or Backward? 
            // Let's do a quick backward phase as a defensive maneuver
            // Or you can make this Vector3.zero for just an invincibility frame logic (not physics)
            dodgeDirection = transform.forward; // Spot dodge jumps "Forward" relative to ship
        }

        // Apply Impulse (Instant force, ignoring mass)
        rb.AddForce(dodgeDirection * currentDodgeForce, ForceMode.Impulse);
    }
    
    // Method to handle Explosions (Call this from your Bomb/Explosion script)
    public void ApplyExplosion(Vector3 position, float force, float radius)
    {
        rb.AddExplosionForce(force, position, radius);
        // This naturally creates the "pushing ship in opposite direction" effect
    }

    #endregion
}