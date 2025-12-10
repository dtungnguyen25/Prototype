using UnityEngine;
using UnityEngine.EventSystems; // Required for touch events

public class MobileJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    [Header("Settings")]
    public RectTransform background;
    public RectTransform handle;

    // This variable acts just like Input.GetAxis()
    // Other scripts can read MobileJoystick.InputDirection.x or .y
    public static Vector2 InputDirection;

    private Vector2 joystickPosition = Vector2.zero;

    void Start()
    {
        InputDirection = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
        // Calculate where the touch is relative to the background
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out pos))
        {
            pos.x = (pos.x / background.sizeDelta.x);
            pos.y = (pos.y / background.sizeDelta.y);

            // Calculate the Input Vector (normalized)
            InputDirection = new Vector2(pos.x * 2, pos.y * 2); // Convert 0..0.5 to 0..1 range

            // Clamp so value doesn't go above 1.0
            InputDirection = (InputDirection.magnitude > 1.0f) ? InputDirection.normalized : InputDirection;

            // Move the Handle Visual
            handle.anchoredPosition = new Vector2(InputDirection.x * (background.sizeDelta.x / 2), InputDirection.y * (background.sizeDelta.y / 2));
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData); // Snap to finger immediately
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Reset to center when finger lifts
        InputDirection = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }
}