using UnityEngine;

public class KatamariController : MonoBehaviour
{
    [Header("Settings")]
    // Tag for objects that can be picked up
    public string stickableTag = "Prop";

    // How much bigger the player needs to be to pick something up (1.0 = same size, 1.2 = 20% bigger)
    public float sizeRequirementRatio = 0.5f;

    // How much to grow per object (very small number!)
    public float growthFactor = 0.05f;

    [Header("Components")]
    public SphereCollider myCollider;

    private void Start()
    {
        if (myCollider == null)
            myCollider = GetComponent<SphereCollider>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 1. Check if the object has the correct tag
        if (collision.gameObject.CompareTag(stickableTag))
        {
            // 2. Check Size logic
            // We use Bounds to guess "visual size" or just check Transform.localScale
            // Here we check if our Mass/Scale is larger than the object's
            float mySize = transform.localScale.x;
            float targetSize = collision.transform.localScale.x;

            if (mySize >= (targetSize * sizeRequirementRatio))
            {
                StickObject(collision.gameObject);
            }
        }
    }

    void StickObject(GameObject obj)
    {
        // --- 1. DISABLE PHYSICS ---
        // We destroy the Rigidbody so gravity/forces stop working on it
        Destroy(obj.GetComponent<Rigidbody>());

        // Optional: Disable the object's collider so it doesn't get snagged on the ground
        // OR: Keep it enabled if you want the ball to roll "lumpy"
        // obj.GetComponent<Collider>().enabled = false; 
        // For a smoother Katamari experience early on, I recommend disabling the collider or setting it to Trigger
        obj.GetComponent<Collider>().isTrigger = true;

        // --- 2. ATTACH TO PLAYER ---
        // This makes the object move and rotate WITH the ball
        obj.transform.SetParent(this.transform);

        // --- 3. GROW PLAYER ---
        transform.localScale += Vector3.one * growthFactor;

        // --- 4. ADJUST CAMERA / PHYSICS (Optional) ---
        // As you get bigger, you might want to increase mass or move camera back
        // GetComponent<Rigidbody>().mass += 0.1f;

        // --- 5. CLEANUP ---
        // Change the tag so we don't try to stick it again
        obj.tag = "Untagged";

        // Play a sound effect here if you have one!
    }
}