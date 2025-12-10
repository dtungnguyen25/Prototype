using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RadarSystem : MonoBehaviour
{
    [Header("References")]
    public Camera mainCam;
    public Transform playerShip;
    public RectTransform radarContainer; // The empty object inside your Crosshair
    public GameObject dotPrefab;         // The Red Dot UI Prefab
    public LayerMask enemyLayer;

    [Header("Settings")]
    public float detectionRadius = 100f; // Max distance to track
    public float uiRadius = 100f;        // How far from center the dots sit (Pixel Radius)

    [Header("Scaling")]
    public float minScale = 0.2f;        // Size when far away
    public float maxScale = 1f;        // Size when very close

    // Internal tracking
    private List<GameObject> activeDots = new List<GameObject>();

    void LateUpdate()
    {
        ClearDots();
        UpdateRadar();
    }

    void UpdateRadar()
    {
        // 1. Find all enemies around the ship
        Collider[] enemies = Physics.OverlapSphere(playerShip.position, detectionRadius, enemyLayer);
        Vector2 radarCenter = new Vector2(Screen.width / 2, Screen.height / 3 * 2);

        foreach (Collider enemy in enemies)
        {
            Vector3 enemyPos = enemy.transform.position;
            Vector3 screenPos = mainCam.WorldToScreenPoint(enemyPos);

            // 2. Check if enemy is BEHIND the camera
            bool isBehind = screenPos.z < 0;

            // 3. Check if enemy is OFF SCREEN (or behind)
            // A quick way to check 'off screen' is if x or y are outside screen bounds
            bool isOffScreen = isBehind ||
                               screenPos.x < 0 || screenPos.x > Screen.width ||
                               screenPos.y < 0 || screenPos.y > Screen.height;

            if (isOffScreen)
            {
                CreateRadarDot(enemy.transform, screenPos, radarCenter, isBehind);
            }
        }
    }

    void CreateRadarDot(Transform target, Vector3 screenPos, Vector2 center, bool isBehind)
    {
        // --- A. CALCULATE DIRECTION ---
        // If behind, we invert the position relative to center to make it point correctly
        if (isBehind)
        {
            screenPos.x = center.x - (screenPos.x - center.x);
            screenPos.y = center.y - (screenPos.y - center.y); // Flip Y as well
        }

        // Get direction from center to the enemy screen position
        Vector2 direction = (new Vector2(screenPos.x, screenPos.y) - center).normalized;

        // --- B. POSITION THE DOT ---
        // Place the dot on the edge of the circle (direction * radius)
        Vector2 uiPos = direction * uiRadius;

        // Instantiate dot
        GameObject dot = Instantiate(dotPrefab, radarContainer);

        // Since 'radarContainer' is centered, local position (0,0) is center of screen.
        dot.GetComponent<RectTransform>().anchoredPosition = uiPos;

        // --- C. ROTATE THE DOT (Optional) ---
        // Make the dot point outward (useful if using an arrow sprite instead of circle)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        dot.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, angle - 90);

        // --- D. SCALE BASED ON DISTANCE ---
        float distToEnemy = Vector3.Distance(playerShip.position, target.position);

        // Inverse Lerp: 0 if at max range, 1 if right next to us
        float t = 1 - Mathf.Clamp01(distToEnemy / detectionRadius);

        // Lerp size between min and max
        float finalScale = Mathf.Lerp(minScale, maxScale, t);
        dot.transform.localScale = Vector3.one * finalScale;

        activeDots.Add(dot);
    }

    void ClearDots()
    {
        foreach (GameObject dot in activeDots)
        {
            Destroy(dot);
        }
        activeDots.Clear();
    }

    // Debug range in editor
    void OnDrawGizmosSelected()
    {
        if (playerShip != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerShip.position, detectionRadius);
        }
    }
}