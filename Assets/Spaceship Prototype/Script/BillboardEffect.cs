using UnityEngine;

public class BillboardEffect : MonoBehaviour
{
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void LateUpdate()
    {
        // Instead of LookAt, we simply match the camera's rotation.
        // This ensures the UI is always parallel to the screen 
        // and its "Up" matches the Camera's "Up".
        transform.rotation = mainCam.transform.rotation;
    }
}