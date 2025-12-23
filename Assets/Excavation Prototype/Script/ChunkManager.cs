using UnityEngine;
using System.Collections.Generic;

public class ChunkManager : MonoBehaviour
{
    public GameObject chunkPrefab;
    public Transform levelPivot;   // <--- NEW: Reference to the thing that spins
    public int gridSize = 8;
    public float spacing = 1.0f;

    private DirtChunk[,,] grid;

    void Start()
    {
        // Safety check: If you forgot to drag the pivot in, use this object's transform
        if (levelPivot == null)
        {
            Debug.LogWarning("Level Pivot not assigned in ChunkManager! Using this object instead.");
            levelPivot = transform;
        }

        GenerateBlock();
    }

    void GenerateBlock()
    {
        grid = new DirtChunk[gridSize, gridSize, gridSize];
        float offset = (gridSize * spacing) / 2f - (spacing / 2f);

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    Vector3 pos = new Vector3(x * spacing, y * spacing, z * spacing);
                    pos -= new Vector3(offset, offset, offset);

                    // Center check (Treasure Zone)
                    float center = gridSize / 2f;
                    if (Vector3.Distance(new Vector3(x, y, z), new Vector3(center, center, center)) < 2.5f)
                        continue;

                    // --- THE FIX IS HERE ---
                    // We now instantiate the chunk as a child of 'levelPivot'
                    GameObject go = Instantiate(chunkPrefab, levelPivot);
                    // -----------------------

                    go.transform.localPosition = pos;
                    go.layer = LayerMask.NameToLayer("Dirt");

                    // Use GetComponent to fix the previous bug too
                    DirtChunk chunkScript = go.GetComponent<DirtChunk>();
                    if (chunkScript == null) chunkScript = go.AddComponent<DirtChunk>(); // Safety add

                    chunkScript.Init(x, y, z, this);
                    grid[x, y, z] = chunkScript;
                }
            }
        }
    }

    public void DestroyChunk(DirtChunk chunk)
    {
        // 1. Remove from grid logic
        grid[chunk.x, chunk.y, chunk.z] = null;

        // 2. Destroy the visual object immediately (The "Pop")
        Destroy(chunk.gameObject);

        // 3. Check for floating islands
        CheckConnectivity();
    }

    // THE ALGORITHM: Breadth-First Search (Flood Fill)
    void CheckConnectivity()
    {
        bool[,,] visited = new bool[gridSize, gridSize, gridSize];
        Queue<DirtChunk> queue = new Queue<DirtChunk>();

        // Step A: Find "Anchor" blocks
        // Any dirt block that touches the "Inner Treasure" is considered grounded (Anchor)
        // We scan the center area of the array to find our starting points
        float center = gridSize / 2f;
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    DirtChunk c = grid[x, y, z];
                    if (c != null)
                    {
                        // If this block is close to the center, it's supported by the treasure
                        if (Vector3.Distance(new Vector3(x, y, z), new Vector3(center, center, center)) < 3.5f)
                        {
                            queue.Enqueue(c);
                            visited[x, y, z] = true;
                        }
                    }
                }
            }
        }

        // Step B: Spread out from Anchors to find all connected blocks
        while (queue.Count > 0)
        {
            DirtChunk current = queue.Dequeue();

            // Check 6 neighbors (Up, Down, Left, Right, Forward, Back)
            CheckNeighbor(current.x + 1, current.y, current.z, visited, queue);
            CheckNeighbor(current.x - 1, current.y, current.z, visited, queue);
            CheckNeighbor(current.x, current.y + 1, current.z, visited, queue);
            CheckNeighbor(current.x, current.y - 1, current.z, visited, queue);
            CheckNeighbor(current.x, current.y, current.z + 1, visited, queue);
            CheckNeighbor(current.x, current.y, current.z - 1, visited, queue);
        }

        // Step C: Drop anything that wasn't visited
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    DirtChunk c = grid[x, y, z];
                    if (c != null && !visited[x, y, z])
                    {
                        // It's floating!
                        c.Drop();
                        grid[x, y, z] = null; // Remove from logic so we don't check it again
                    }
                }
            }
        }
    }

    void CheckNeighbor(int x, int y, int z, bool[,,] visited, Queue<DirtChunk> queue)
    {
        // Boundary checks
        if (x < 0 || x >= gridSize || y < 0 || y >= gridSize || z < 0 || z >= gridSize) return;

        // If there is a block and we haven't visited it yet
        if (grid[x, y, z] != null && !visited[x, y, z])
        {
            visited[x, y, z] = true;
            queue.Enqueue(grid[x, y, z]);
        }
    }
}