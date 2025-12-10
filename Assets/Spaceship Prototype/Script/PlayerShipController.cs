using UnityEngine;

public class PlayerShipController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float forwardSpeed = 20f;  // Speed of the auto-flight
    public float turnSpeed = 60f;     // How fast the ship turns

    [Header("Auto Level Settings")]
    public float autoLevelSpeed = 2.0f; // Higher = Snaps back faster


    void Update()
    {
        // 1. AUTO-FORWARD
        // This moves the ship forward every single frame based on its current facing direction.
        // Time.deltaTime ensures it moves smoothly regardless of framerate.
        transform.Translate(Vector3.forward * forwardSpeed * Time.deltaTime);

        // 2. INPUT (Simulation)
        // We use "Horizontal" (A/D or Left/Right) and "Vertical" (W/S or Up/Down)
        // to simulate your future Virtual Joystick.
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // --- JOYSTICK OVERRIDE ---
        // If the MobileJoystick script reports input (magnitude > 0), use that instead.
        // We check if the vector is not zero.
        if (MobileJoystick.InputDirection.sqrMagnitude > 0.001f)
        {
            horizontal = MobileJoystick.InputDirection.x;
            vertical = MobileJoystick.InputDirection.y;
        }

        // 3. STEERING
        // We calculate how much to rotate based on input and turn speed.
        // "vertical" rotates around X axis (pitch up/down).
        // "horizontal" rotates around Y axis (turn left/right).
        Vector3 rotateAmount = new Vector3(vertical * -1f, horizontal, 0) * turnSpeed * Time.deltaTime;

        // Apply the rotation to the ship
        transform.Rotate(rotateAmount);

        HandleAutoLevel(); // Auto-leveling function
    }
    void HandleAutoLevel()
    {
        // 1. Check if the player is currently rolling/turning. 
        // CHANGE "Horizontal" to whatever input name you use for turning/rolling!
        float rotationInput = Input.GetAxis("Horizontal");

        // 2. Only auto-level if input is very small (player let go)
        if (Mathf.Abs(rotationInput) < 0.1f)
        {
            // 3. Create a target rotation: 
            // "Look in my current forward direction, but keep my head pointing to the sky (Vector3.up)"
            Quaternion targetRotation = Quaternion.LookRotation(transform.forward, Vector3.up);

            // 4. Smoothly interpolate (Slerp) from current rotation to the target rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * autoLevelSpeed);
        }
    }
}


