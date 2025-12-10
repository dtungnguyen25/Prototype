using UnityEngine;

public class SpaceProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 50f;
    public float lifeTime = 5f;
    public int damage = 10;

    [Header("Homing Settings")]
    public bool isHoming = false;
    public float homingTurnSpeed = 2f; // How hard it can turn

    private Transform target;

    void Start()
    {
        // Destroy the bullet automatically after 'lifeTime' seconds
        Destroy(gameObject, lifeTime);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void Update()
    {
        MoveProjectile();
    }

    void MoveProjectile()
    {
        if (isHoming && target != null)
        {
            // 1. Calculate direction to target
            Vector3 direction = (target.position - transform.position).normalized;

            // 2. Create the rotation we want to have
            Quaternion lookRotation = Quaternion.LookRotation(direction);

            // 3. Smoothly rotate towards that rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * homingTurnSpeed);
        }

        // Move forward constantly based on current rotation
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the object we hit has a HealthSystem
        HealthSystem health = other.GetComponent<HealthSystem>();

        if (health != null)
        {
            health.TakeDamage(damage);
        }

        // Destroy bullet on impact (unless it's the player shooting themselves)
        // Add a tag check here later so you don't shoot yourself!
        Destroy(gameObject);
    }
}