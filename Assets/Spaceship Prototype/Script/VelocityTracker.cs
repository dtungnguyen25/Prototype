using UnityEngine;
using System.Collections.Generic;

public class VelocityTracker : MonoBehaviour
{
    [Header("Smoothing Settings")]
    public int samples = 5; // Higher = Smoother but more "laggy"

    // This is the value your Targeting System should read!
    public Vector3 SmoothedVelocity { get; private set; }

    private Queue<Vector3> velocitySamples = new Queue<Vector3>();
    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        // 1. Calculate raw velocity for this one frame
        Vector3 rawVelocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;

        // 2. Add to our history buffer
        velocitySamples.Enqueue(rawVelocity);

        // 3. Keep the buffer size limited
        if (velocitySamples.Count > samples)
        {
            velocitySamples.Dequeue();
        }

        // 4. Calculate the average
        Vector3 sum = Vector3.zero;
        foreach (Vector3 v in velocitySamples)
        {
            sum += v;
        }

        SmoothedVelocity = sum / velocitySamples.Count;
    }
}
