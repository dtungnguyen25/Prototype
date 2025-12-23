using UnityEngine;

public class DirtChunk : MonoBehaviour
{
    public int x, y, z; // My coordinates in the grid
    public ChunkManager manager;
    private bool isDropped = false;

    // Setup function called by the Generator
    public void Init(int _x, int _y, int _z, ChunkManager _manager)
    {
        x = _x; y = _y; z = _z;
        manager = _manager;
    }

    // Called by the Player Input
    public void OnHit()
    {
        if (isDropped) return;

        // Tell the manager to destroy me and check physics for neighbors
        manager.DestroyChunk(this);
    }

    // Called by the Manager if this chunk is found to be floating
    public void Drop()
    {
        if (isDropped) return;
        isDropped = true;

        // Visual: Unparent and fall
        transform.SetParent(null);
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = true;

        // Optional: Add a slight push
        rb.AddForce(Random.insideUnitSphere * 2f, ForceMode.Impulse);

        // Cleanup
        Destroy(gameObject, 3f);
    }
}