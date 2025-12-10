using UnityEngine;
using System.Collections;

public class SpawnManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public GameObject[] enemyPrefabs;   // Drag all your different enemy types here
    public GameObject spawnVFXPrefab;   // The visual effect that plays during the 2s delay

    [Header("Spawn Settings")]
    public float spawnRadius = 100f;     // How far from player to spawn
    public int maxEnemies = 10;         // Cap: Don't spawn if more than this exist
    public float spawnInterval = 3f;    // Time between spawn attempts
    public float warpDelay = 2.0f;      // How long the warning lasts before enemy appears

    private float nextSpawnTime;

    void Update()
    {
        // Only try to spawn if enough time has passed
        if (Time.time >= nextSpawnTime)
        {
            // Check how many enemies are currently alive
            int currentEnemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

            if (currentEnemyCount < maxEnemies)
            {
                StartCoroutine(SpawnEnemyRoutine());
                nextSpawnTime = Time.time + spawnInterval;
            }
        }
    }

    IEnumerator SpawnEnemyRoutine()
    {
        // 1. Calculate a random position around the player
        // 'onUnitSphere' gives a random point on a radius of 1. We multiply by our radius.
        Vector3 randomPos = player.position + (Random.onUnitSphere * spawnRadius);

        // 2. Instantiate the "Warning" Effect (The Radar will see this!)
        GameObject warningVFX = Instantiate(spawnVFXPrefab, randomPos, Quaternion.identity);

        // 3. Wait for 2 seconds (The visual effect plays, radar shows a dot)
        yield return new WaitForSeconds(warpDelay);

        // 4. Cleanup the warning effect
        Destroy(warningVFX);

        // 5. Spawn the Real Enemy at that same position
        int randomIndex = Random.Range(0, enemyPrefabs.Length);
        Instantiate(enemyPrefabs[randomIndex], randomPos, Quaternion.LookRotation(player.position - randomPos));
    }

    // Visualize the spawn sphere in Editor
    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.position, spawnRadius);
        }
    }
}