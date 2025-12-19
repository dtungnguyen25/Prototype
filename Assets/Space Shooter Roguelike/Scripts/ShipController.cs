using NUnit.Framework.Internal.Commands;
using UnityEngine;
using UnityEngine.InputSystem; // Required for the New Input System

[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("The camera the ship moves relative to.")]
    public Transform cameraTransform;

    [Header("Movement Stats")]
    public float acceleration = 40f;      // How fast we speed up
    public float maxSpeed = 20f;          // Normal top speed
    public float boostSpeed = 40f;        // Top speed while boosting
    public float brakeStrength = 5f;      // How fast the support brake stops us
    
    [Header("Rotation Stats")]
    public float turnSpeed = 5f;          // How fast we turn with Right Stick
    public float autoLevelSpeed = 2f;     // How fast we auto-level to horizon
    public float autoLevelDelay = 2.0f; // How long to wait (2 seconds)
    public float maxSpeedForAutoLevel = 10f; // Only auto-level if below this speed
    private float lastInputTime;        // The timestamp of when we last stopped turning
    
    [Header("Dodge & Energy")]
    public float dodgeForce = 50f;        // Instant force applied when dodging
    public float energyPerDodge = 25f;     // Energy cost per dodge
    public float maxEnergy = 100f;        // Total energy for boosting
    public float energyDrainRate = 20f;   // Energy lost per second while boosting
    public float energyRechargeRate = 10f;// Energy gained per second
    public float energyRechargeDelay = 2.0f; // Delay before recharging starts
    private float lastEnergyUseTime; // Timestamp of last energy use
    
    // Internal State Flags
    private bool isBoosting = false;
    private bool isBrakingEnabled = true; // Can be disabled later for damage effects
    private float currentEnergy;

    // Input Storage
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float thrustForwardInput;
    private float thrustBackwardInput;

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

        currentEnergy = maxEnergy;
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
            currentEnergy -= energyDrainRate * Time.deltaTime; // Drain energy
            lastEnergyUseTime = Time.time; // Update last use time
        }
        else
        {
            // Stop boosting if out of energy
            if (currentEnergy <= 0) isBoosting = false;
            
            // CHECK: Has enough time passed since we last used energy?
            if (Time.time > lastEnergyUseTime + energyRechargeDelay)
            {
            // Recharge
            currentEnergy = Mathf.Clamp(currentEnergy + energyRechargeRate * Time.deltaTime, 0, maxEnergy);
            }
        }
    }

    private void HandleMovement()
    {
        // 1. Calculate Thrust (LT - LB)
        float thrustAxis = thrustForwardInput - thrustBackwardInput;
        Vector3 thrustForce = transform.forward * thrustAxis * acceleration;

        // 2. Calculate Strafe (Left Stick relative to Camera)
        // We project the camera's up and right vectors onto a flat plane so looking down doesn't mess up movement
        Vector3 camUp = cameraTransform.up;
        Vector3 camRight = cameraTransform.right;

        Vector3 strafeForce = (camUp * moveInput.y + camRight * moveInput.x) * acceleration;

        // 3. Apply Forces
        float speedLimit = isBoosting ? boostSpeed : maxSpeed;
        
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
        Vector3 torque = new Vector3(lookInput.y * -1f, lookInput.x, 0) * turnSpeed; // invert Y here if needed
        
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
    bool isMovingFast = currentSpeed > maxSpeedForAutoLevel;

    if (isRotating || isMovingFast)
    {
        // If we are rotating, reset the "Last Input Time" to right now
        lastInputTime = Time.time;
    }
    else
    {
        // 2. We are NOT rotating. Check how much time has passed.
        float timeSinceInput = Time.time - lastInputTime;

        if (timeSinceInput > autoLevelDelay)
        {
            // 3. The Delay is over! Apply Stabilization.
            
            // Calculate the rotation needed to align 'up' with 'Vector3.up'
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, Vector3.up);
            
            targetRotation.ToAngleAxis(out float angle, out Vector3 axis);

            // Normalize angle to handle the -180 to 180 wrap-around
            if (angle > 180) angle -= 360;

            // Apply the Torque (Physics force) to gently rotate us back
            // We use the same 'stabilizeSpeed' variable from your stats
            rb.AddTorque(axis * (angle * autoLevelSpeed * Mathf.Deg2Rad) - rb.angularVelocity);
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
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, brakeStrength * Time.fixedDeltaTime);
            
            // Also kill rotation if joystick is let go
            if(lookInput.sqrMagnitude < 0.1f)
            {
                rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, brakeStrength * Time.fixedDeltaTime);
            }
        }
    }

    private void PerformDodge()
    {
        // 1. Check if we have enough energy
    if (currentEnergy < energyPerDodge)
    {
        // To be doing: Play a "failed" sound or flash UI here
        return; 
    }

        // 2. Pay the cost (Instant deduction)
        currentEnergy -= energyPerDodge;
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
        rb.AddForce(dodgeDirection * dodgeForce, ForceMode.Impulse);
    }
    
    // Method to handle Explosions (Call this from your Bomb/Explosion script)
    public void ApplyExplosion(Vector3 position, float force, float radius)
    {
        rb.AddExplosionForce(force, position, radius);
        // This naturally creates the "pushing ship in opposite direction" effect
    }

    #endregion
}