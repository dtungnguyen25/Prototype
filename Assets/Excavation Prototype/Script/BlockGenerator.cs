using UnityEngine;
using System.Collections.Generic;

public class BlockGenerator : MonoBehaviour
{
    public GameObject chunkPrefab; // Assign a small cube prefab here
    public int gridSize = 8;       // Size of the block (e.g. 8x8x8)
    public float spacing = 1.0f;   // Size of the chunk prefab

    // Keep track of active chunks for the Win Condition
    public List<GameObject> activeChunks = new List<GameObject>();

    void Start()
    {
        GenerateBlock();
    }

    void GenerateBlock()
    {
        float offset = (gridSize * spacing) / 2f - (spacing / 2f);

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    // Calculate position so the block is centered at (0,0,0)
                    Vector3 pos = new Vector3(x * spacing, y * spacing, z * spacing);
                    pos -= new Vector3(offset, offset, offset);

                    // Don't spawn chunks INSIDE the treasure (Simple check)
                    // You can check distance to center if you want a hollow sphere
                    // if (Vector3.Distance(pos, Vector3.zero) < 1.5f) continue;

                    GameObject chunk = Instantiate(chunkPrefab, transform);
                    chunk.transform.localPosition = pos;
                    chunk.layer = LayerMask.NameToLayer("Dirt"); // Important for Raycast

                    // Add DirtChunk script
                    chunk.AddComponent<DirtChunk>();

                    activeChunks.Add(chunk);
                }
            }
        }
    }
}