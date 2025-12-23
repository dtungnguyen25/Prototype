using UnityEngine;

public class ExcavationController : MonoBehaviour
{
    public Transform levelPivot;
    public float rotationSpeed = 100f; // Increased default speed
    public LayerMask dirtLayer;

    private Vector3 lastMousePosition;
    private bool isDragging = false;

    void Update()
    {
        // 1. Check if LevelPivot is assigned
        if (levelPivot == null)
        {
            Debug.LogError("ERROR: 'Level Pivot' is empty! Please assign it in the Inspector.");
            return;
        }

        // 2. Check Input Start
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Debug Draw: Shows a red line in the Scene view for the click
            Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 1f);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, dirtLayer))
            {
                Debug.Log("Clicked on Dirt - Digging Logic");
                // Digging logic (simplified for debug focus)
                DirtChunk chunk = hit.collider.GetComponent<DirtChunk>();
                if (chunk != null) chunk.OnHit();
            }
            else
            {
                Debug.Log("Clicked on Empty Space - Start Dragging");
                isDragging = true;
            }
        }

        // 3. Handle Dragging
        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;

            // If delta is very small, we might not see rotation
            if (delta.magnitude > 0)
            {
                Debug.Log($"Dragging: Delta {delta}");
            }

            // Note: Removed Time.deltaTime for snappier mouse control, 
            // or use a much higher speed (like 100-200) if keeping deltaTime.
            float rotX = -delta.x * rotationSpeed * Time.deltaTime;
            float rotY = delta.y * rotationSpeed * Time.deltaTime;

            levelPivot.Rotate(Vector3.up, rotX, Space.World);
            levelPivot.Rotate(Vector3.right, rotY, Space.World);

            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging) Debug.Log("Stopped Dragging");
            isDragging = false;
        }
    }
bool IsTreasureFree()
    {
        // Fire rays from the center of the treasure (0,0,0) in 6 directions
        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

        int blockedSides = 0;

        foreach (Vector3 dir in directions)
        {
            // Raycast starting from 0,0,0 going outwards
            // Using a max distance slightly larger than the treasure size
            if (Physics.Raycast(Vector3.zero, dir, 2.0f, dirtLayer))
            {
                blockedSides++;
            }
        }

        // If 0 sides are blocked, the object is basically free!
        if (blockedSides == 0)
        {
            Debug.Log("YOU WIN! Treasure Extracted!");
            return true;
        }
        return false;
    }
}