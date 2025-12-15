using UnityEngine;

// This line ensures Unity won't let you attach this script to an object without a Rigidbody
[RequireComponent(typeof(Rigidbody))]
public class KatamariMovement : MonoBehaviour
{
    public float speed = 50f;

    private Rigidbody rb;
    private Transform cameraTransform;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Find the main camera to make controls intuitive
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("Main Camera not found! Tag your camera as 'MainCamera'.");
        }
    }

    void FixedUpdate()
    {
        // 1. Get Input
        // If this throws an error, see the "Input Settings" step below
        float moveH = Input.GetAxis("Horizontal");
        float moveV = Input.GetAxis("Vertical");

        // 2. Calculate movement direction relative to the Camera
        Vector3 movement = Vector3.zero;

        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            // Flatten the vectors so we don't try to roll into the ground
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            // Combine inputs with camera direction
            movement = (camForward * moveV + camRight * moveH).normalized;
        }
        else
        {
            // Fallback if no camera found
            movement = new Vector3(moveH, 0, moveV);
        }

        // 3. Apply Torque (Roll)
        // We cross product the direction to get the axis of rotation
        if (movement.magnitude > 0.1f)
        {
            // To roll forward (Z), we need torque on the X axis.
            // To roll right (X), we need torque on the Z axis (negative).
            Vector3 torqueDirection = new Vector3(movement.z, 0, -movement.x);
            rb.AddTorque(torqueDirection * speed);
        }
    }
}