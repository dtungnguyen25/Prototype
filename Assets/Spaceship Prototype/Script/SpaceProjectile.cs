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

    [Tooltip("Manually assign target (optional). If empty, script tries to find Player tag.")]
    public Transform target; 

    void Start()
    {
        // If isHoming is true, but you forgot to assign a target, find the Player automatically.
        if (isHoming && target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }

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
            // We use Slerp first to verify we have a valid direction to avoid errors if target is exactly inside us
            if (direction != Vector3.zero) 
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                
                // 3. Smoothly rotate towards that rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * homingTurnSpeed);
            }
        }

        // Move forward constantly based on current rotation
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the object we hit has a HealthSystem
        HealthSystem health = other.GetComponent<HealthSystem>();

        // Optional: specific tag check to avoid hurting the enemy who shot it
        // if (health != null && !other.CompareTag("Enemy")) 
        if (health != null)
        {
            health.TakeDamage(damage);
        }

        // Destroy bullet on impact
        Destroy(gameObject);
    }
}