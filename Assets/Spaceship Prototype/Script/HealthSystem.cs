using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthSystem : MonoBehaviour
{
    [Header("Settings")]
    public float maxHealth = 120f;
    public bool isPlayer = false;
    public float respawnDelay = 2.0f;

    [Header("References")]
    public Image healthBarFill; // Drag the filling image here

    private float currentHealth;
    private bool isDead = false;

    // Store starting position for respawning
    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        currentHealth = maxHealth;
        startPosition = transform.position;
        startRotation = transform.rotation;
        UpdateHealthUI();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateHealthUI()
    {
        if (healthBarFill != null)
        {
            // Calculate percentage (0.0 to 1.0)
            healthBarFill.fillAmount = currentHealth / maxHealth;
        }
    }

    void Die()
    {
        if (isPlayer)
        {
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            // Add explosion particles here later!
            Destroy(gameObject);
        }
    }

    IEnumerator RespawnRoutine()
    {
        isDead = true;

        // 1. Hide the player visually and physically
        SetPlayerActive(false);

        // 2. Wait
        yield return new WaitForSeconds(respawnDelay);

        // 3. Reset Position and Health
        transform.position = startPosition;
        transform.rotation = startRotation;
        currentHealth = maxHealth;
        UpdateHealthUI();

        // 4. Re-enable
        SetPlayerActive(true);
        isDead = false;
    }

    void SetPlayerActive(bool isActive)
    {
        // Disable Colliders and Renderers so player is "invisible" but script still runs
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = isActive;
        foreach (var rend in GetComponentsInChildren<Renderer>()) rend.enabled = isActive;

        // Disable Movement Scripts (Change names if your scripts are named differently!)
        if (GetComponent<MonoBehaviour>() != null)
            GetComponent<MonoBehaviour>().enabled = isActive;

        // IMPORTANT: Make sure THIS script stays enabled so the Coroutine finishes!
        this.enabled = true;
    }
}