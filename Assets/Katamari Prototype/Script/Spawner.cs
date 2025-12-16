using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Settings")]
    public GameObject[] prefabsToSpawn; // Drag your props (cows, fences, apples) here
    public Transform playerTransform;   // Drag your Player (Katamari ball) here

    [Header("Spawn Logic")]
    public float spawnRadius = 20f;     // How far away from player to spawn
    public float spawnInterval = 1.0f;  // How fast to spawn objects (seconds)
    public LayerMask groundLayer;       // What is considered "ground"?

    [Header("Positioning")]
    public bool spawnOnGround = true;   // TRUE = snap to floor. FALSE = float in air.
    public float heightOffset = 0.5f;   // Lift object slightly so it doesn't clip into ground

    [Header("Randomization")]
    public float minScale = 0.5f;
    public float maxScale = 1.5f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnObject();
            timer = 0f;
        }
    }

    void SpawnObject()
    {
        if (prefabsToSpawn.Length == 0 || playerTransform == null) return;

        // 1. Pick a random prefab
        GameObject prefab = prefabsToSpawn[Random.Range(0, prefabsToSpawn.Length)];

        // 2. Find a random point around the player (X, Z plane)
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = new Vector3(
            playerTransform.position.x + randomCircle.x,
            playerTransform.position.y + 10f, // Start high up for the raycast
            playerTransform.position.z + randomCircle.y
        );

        // 3. Raycast down to find the ground
        RaycastHit hit;
        // We cast a ray from high up (spawnPos) downwards
        if (Physics.Raycast(spawnPos, Vector3.down, out hit, 50f, groundLayer))
        {
            Vector3 finalPosition = hit.point;

            // Apply height offset (so it sits ON the ground, not IN it)
            if (spawnOnGround)
            {
                finalPosition.y += heightOffset;
            }
            else
            {
                // If you want floating objects, keep the random Y height or set a fixed height
                finalPosition.y = playerTransform.position.y + Random.Range(1f, 3f);
            }

            // 4. Spawn the object (Use your ObjectPoolingManager here if you want!)
            GameObject newObj = Instantiate(prefab, finalPosition, Quaternion.identity);

            // 5. Randomize Rotation and Scale
            ApplyRandomness(newObj);
        }
    }

    void ApplyRandomness(GameObject obj)
    {
        // Random Rotation (usually just Y axis for ground objects, or random for space objects)
        obj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

        // Random Scale
        float scale = Random.Range(minScale, maxScale);
        obj.transform.localScale = Vector3.one * scale;
    }

    // This draws a Gizmo in the scene view so you can see the spawn radius
    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTransform.position, spawnRadius);
        }
    }
}