using UnityEngine;
using UnityEngine.UI;

public class WorldToScreenTracker : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Drag the 3D Sphere (your crosshair object) here")]
    public Transform PhysicalCrosshair3D;
    
    [Tooltip("Drag your Main Camera here")]
    public Camera MainCamera;

    private RectTransform rectTransform;
    private Image crosshairImage;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        crosshairImage = GetComponent<Image>();
    }

    private void LateUpdate()
    {
        if (PhysicalCrosshair3D == null || MainCamera == null) return;

        // 1. Check if the sphere is actually in front of the camera
        // (If it's behind us, we should hide the UI)
        Vector3 screenPos = MainCamera.WorldToScreenPoint(PhysicalCrosshair3D.position);
        bool isBehind = screenPos.z < 0;

        if (isBehind)
        {
            crosshairImage.enabled = false;
        }
        else
        {
            crosshairImage.enabled = true;
            // 2. Snap UI position to the Screen Position of the sphere
            rectTransform.position = screenPos;
        }
    }
}