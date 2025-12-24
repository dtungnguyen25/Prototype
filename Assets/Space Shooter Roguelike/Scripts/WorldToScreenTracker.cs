using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro; // For TextMeshPro (better text rendering)

// ============================================================================
// WORLD TO SCREEN TRACKER - LOCK-ON SYSTEM
// ============================================================================

/// <summary>
/// Projects 3D targets to 2D screen space for lock-on indicators.
/// Handles crosshair, tracking indicators, locked indicators, and primary target displays.
/// </summary>
public class WorldToScreenTracker : MonoBehaviour
{
    // ========================================================================
    // CROSSHAIR SETUP
    // ========================================================================

    [Header("Crosshair Setup")]
    [Tooltip("The physical 3D crosshair object in the world.")]
    public Transform PhysicalCrosshair3D;

    [Tooltip("UI Image for the crosshair.")]
    public Image CrosshairImage;

    [Tooltip("Smoothing factor for crosshair movement (0 = instant, 10 = smooth, 20 = very smooth).")]
    [Range(0f, 20f)]
    public float CrosshairSmoothSpeed = 10f;

    [Tooltip("Minimum distance from screen edge before hiding crosshair (pixels).")]
    public float EdgeBuffer = 50f;

    // ========================================================================
    // REFERENCES
    // ========================================================================

    [Header("References")]
    [Tooltip("Main camera (auto-found if not assigned).")]
    public Camera MainCamera;

    [Tooltip("Canvas containing all UI elements (auto-found if not assigned).")]
    public Canvas UICanvas;

    [Tooltip("The weapon controller to get target data from.")]
    public WeaponController weaponController;

    [Tooltip("Ship health system for damage-based system degradation (optional).")]
    public HealthSystem shipHealthSystem;

    // ========================================================================
    // LOCK-ON INDICATOR PREFABS
    // ========================================================================

    [Header("Lock-On Indicator Prefabs")]
    [Tooltip("Prefab for targets being tracked (yellow brackets with lock progress).")]
    public GameObject TrackingIndicatorPrefab;

    [Tooltip("Prefab for locked targets (red brackets).")]
    public GameObject LockedIndicatorPrefab;

    [Tooltip("Prefab for primary targets (green brackets with health bar and distance).")]
    public GameObject PrimaryTargetIndicatorPrefab;

    // ========================================================================
    // SYSTEM DEGRADATION (OPTIONAL)
    // ========================================================================

    [Header("System Degradation (Optional)")]
    [Tooltip("Enable damage-based lock-on system malfunction.")]
    public bool EnableSystemDegradation = true;

    [Tooltip("Health percentage below which lock-on starts malfunctioning (0.3 = 30%).")]
    [Range(0f, 1f)]
    public float LockOnMalfunctionThreshold = 0.3f;

    [Tooltip("Flicker speed when systems are damaged.")]
    public float SystemFlickerSpeed = 5f;

    // ========================================================================
    // INTERNAL STATE
    // ========================================================================

    private RectTransform crosshairRect;
    private Vector3 smoothedCrosshairPos;

    // Pooled indicator objects
    private Dictionary<Transform, IndicatorData> activeIndicators = new Dictionary<Transform, IndicatorData>();
    private Dictionary<IndicatorType, Stack<GameObject>> indicatorPools = new Dictionary<IndicatorType, Stack<GameObject>>();

    // System state
    private bool lockOnSystemActive = true;
    private float systemFlickerTimer = 0f;

    // Helper class to store indicator data
    private class IndicatorData
    {
        public GameObject gameObject;
        public RectTransform rectTransform;
        public IndicatorType type;
        public Image lockProgressBar;
        public Image healthBar;
        public TextMeshProUGUI distanceText;
    }

    private enum IndicatorType
    {
        Tracking,  // Yellow - locking in progress
        Locked,    // Red - fully locked
        Primary    // Green - will be fired at
    }

    // ========================================================================
    // INITIALIZATION
    // ========================================================================

    private void Awake()
    {
        // Auto-find references if not assigned
        if (MainCamera == null)
        {
            MainCamera = Camera.main;
            if (MainCamera == null)
            {
                Debug.LogError("WorldToScreenTracker: No camera found! Assign MainCamera.");
            }
        }

        if (UICanvas == null)
        {
            UICanvas = GetComponentInParent<Canvas>();
            if (UICanvas == null)
            {
                Debug.LogError("WorldToScreenTracker: No canvas found! This script must be on a UI element.");
            }
        }

        // Setup crosshair
        if (CrosshairImage != null)
        {
            crosshairRect = CrosshairImage.GetComponent<RectTransform>();
        }

        // Initialize smoothed crosshair position
        if (PhysicalCrosshair3D != null && MainCamera != null)
        {
            smoothedCrosshairPos = MainCamera.WorldToScreenPoint(PhysicalCrosshair3D.position);
        }

        // Validate weapon controller
        if (weaponController == null)
        {
            Debug.LogWarning("WorldToScreenTracker: WeaponController not assigned. Lock-on indicators will not work.");
        }
    }

    // ========================================================================
    // UPDATE LOOP
    // ========================================================================

    private void LateUpdate()
    {
        // Always update crosshair (independent of lock-on system)
        UpdateCrosshair();

        // Update lock-on indicators (if system active and weapon controller exists)
        if (lockOnSystemActive && weaponController != null)
        {
            UpdateLockOnIndicators();
        }
        else
        {
            HideAllIndicators();
        }
    }

    // ========================================================================
    // CROSSHAIR UPDATE (STABILIZED - NO JITTER)
    // ========================================================================

    /// <summary>
    /// Updates crosshair position with smooth interpolation to eliminate jittering.
    /// </summary>
    private void UpdateCrosshair()
    {
        if (PhysicalCrosshair3D == null || MainCamera == null || CrosshairImage == null) return;

        // Convert 3D position to screen space
        Vector3 worldPos = PhysicalCrosshair3D.position;
        Vector3 screenPos = MainCamera.WorldToScreenPoint(worldPos);

        // Check if behind camera
        bool isBehind = screenPos.z < 0;

        if (isBehind)
        {
            CrosshairImage.enabled = false;
            return;
        }

        // Check if outside screen bounds (with buffer)
        bool isOutOfBounds = screenPos.x < EdgeBuffer ||
                            screenPos.x > Screen.width - EdgeBuffer ||
                            screenPos.y < EdgeBuffer ||
                            screenPos.y > Screen.height - EdgeBuffer;

        if (isOutOfBounds)
        {
            CrosshairImage.enabled = false;
            return;
        }

        // Smooth interpolation to eliminate jitter
        if (CrosshairSmoothSpeed > 0)
        {
            smoothedCrosshairPos = Vector3.Lerp(
                smoothedCrosshairPos,
                screenPos,
                Time.deltaTime * CrosshairSmoothSpeed
            );
        }
        else
        {
            // Instant snap (no smoothing)
            smoothedCrosshairPos = screenPos;
        }

        // Update UI position
        CrosshairImage.enabled = true;
        crosshairRect.position = smoothedCrosshairPos;
    }

    // ========================================================================
    // LOCK-ON INDICATORS
    // ========================================================================

    /// <summary>
    /// Updates all lock-on target indicators based on weapon controller state.
    /// Handles tracking (yellow), locked (red), and primary (green) indicators.
    /// </summary>
    private void UpdateLockOnIndicators()
    {
        var allTargets = weaponController.GetActiveTargets();
        var primaryTargets = weaponController.GetPrimaryTargets();

        if (allTargets == null)
        {
            HideAllIndicators();
            return;
        }

        HashSet<Transform> updatedTargets = new HashSet<Transform>();

        foreach (var target in allTargets)
        {
            if (target.transform == null) continue;

            updatedTargets.Add(target.transform);
            IndicatorType type = DetermineIndicatorType(target, primaryTargets);
            UpdateTargetIndicator(target, type);
        }

        CleanupOldIndicators(updatedTargets);
    }

    /// <summary>
    /// Determines what type of indicator to show for a target.
    /// Green = Primary (will be fired at)
    /// Red = Locked (fully locked, ready)
    /// Yellow = Tracking (locking in progress)
    /// </summary>
    private IndicatorType DetermineIndicatorType(
        WeaponController.TargetTrackData target,
        List<WeaponController.TargetTrackData> primaryTargets)
    {
        // Priority 1: Check if this is a primary target (green - will be fired at)
        if (primaryTargets != null && primaryTargets.Contains(target))
        {
            return IndicatorType.Primary;
        }
        // Priority 2: Check if fully locked (red)
        else if (target.isLocked)
        {
            return IndicatorType.Locked;
        }
        // Priority 3: Otherwise tracking (yellow - locking in progress)
        else
        {
            return IndicatorType.Tracking;
        }
    }

    /// <summary>
    /// Updates or creates an indicator for a specific target.
    /// Handles position projection and visual updates.
    /// </summary>
    private void UpdateTargetIndicator(WeaponController.TargetTrackData target, IndicatorType type)
    {
        // Get or create indicator
        IndicatorData indicator = GetOrCreateIndicator(target.transform, type);
        if (indicator == null) return;

        // Convert target world position to screen space
        Vector3 screenPos = MainCamera.WorldToScreenPoint(target.transform.position);

        // Check if behind camera or off-screen
        bool isBehind = screenPos.z < 0;
        bool isOffScreen = screenPos.x < 0 || screenPos.x > Screen.width ||
                          screenPos.y < 0 || screenPos.y > Screen.height;

        if (isBehind || isOffScreen)
        {
            indicator.gameObject.SetActive(false);
            return;
        }

        // Update indicator position
        indicator.gameObject.SetActive(true);
        indicator.rectTransform.position = screenPos;

        // Update indicator visuals based on state
        UpdateIndicatorVisuals(indicator, target, type);
    }

    /// <summary>
    /// Gets existing indicator or creates a new one from pool.
    /// Automatically handles type changes (e.g., tracking → locked → primary).
    /// </summary>
    private IndicatorData GetOrCreateIndicator(Transform target, IndicatorType type)
    {
        if (activeIndicators.TryGetValue(target, out IndicatorData existing))
        {
            if (existing.type != type)
            {
                // CRITICAL FIX: Pass the 'type' so we know which pool to put it in
                ReturnToPool(existing.gameObject, existing.type);
                activeIndicators.Remove(target);
                return CreateNewIndicator(target, type);
            }
            return existing;
        }

        return CreateNewIndicator(target, type);
    }

    /// <summary>
    /// Creates a new indicator GameObject from pool or instantiates new one.
    /// </summary>
    private IndicatorData CreateNewIndicator(Transform target, IndicatorType type)
    {
        // 1. Try to get specific type from pool
        GameObject indicatorObj = GetFromPool(type);

        // 2. If pool empty, instantiate new
        if (indicatorObj == null)
        {
            GameObject prefab = GetIndicatorPrefab(type);
            if (prefab == null) return null;

            indicatorObj = Instantiate(prefab, UICanvas.transform);
        }

        // 3. Set name (This is now safe because we don't rely on name for pooling)
        indicatorObj.name = $"{type}_{target.name}";
        indicatorObj.SetActive(true); // Ensure it's active

        IndicatorData indicator = new IndicatorData
        {
            gameObject = indicatorObj,
            rectTransform = indicatorObj.GetComponent<RectTransform>(),
            type = type,
            lockProgressBar = indicatorObj.transform.Find("LockProgress")?.GetComponent<Image>(),
            healthBar = indicatorObj.transform.Find("HealthBar")?.GetComponent<Image>(),
            distanceText = indicatorObj.transform.Find("DistanceText")?.GetComponent<TextMeshProUGUI>()
        };

        activeIndicators[target] = indicator;
        return indicator;
    }

    /// <summary>
    /// Updates indicator visuals (lock progress, health bars, distance text).
    /// </summary>
    private void UpdateIndicatorVisuals(
        IndicatorData indicator,
        WeaponController.TargetTrackData target,
        IndicatorType type)
    {
        // Update lock progress bar (for tracking indicators)
        if (type == IndicatorType.Tracking && indicator.lockProgressBar != null)
        {
            float lockProgress = target.lockTimer / weaponController.data.LockOnTimeNeeded;
            indicator.lockProgressBar.fillAmount = Mathf.Clamp01(lockProgress);
        }

        // Update health bar (for primary targets)
        if (type == IndicatorType.Primary && indicator.healthBar != null)
        {
            IDamageable damageable = target.transform.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float healthPercent = damageable.GetHealthPercent();
                indicator.healthBar.fillAmount = healthPercent;

                // Color code health bar
                if (healthPercent > 0.6f)
                    indicator.healthBar.color = Color.green;
                else if (healthPercent > 0.3f)
                    indicator.healthBar.color = Color.yellow;
                else
                    indicator.healthBar.color = Color.red;
            }
        }

        // Update distance text (for primary targets)
        if (type == IndicatorType.Primary && indicator.distanceText != null)
        {
            float distance = Vector3.Distance(MainCamera.transform.position, target.transform.position);
            indicator.distanceText.text = $"{distance:F0}m";
        }
    }

    /// <summary>
    /// Removes indicators for targets no longer being tracked.
    /// </summary>
    private void CleanupOldIndicators(HashSet<Transform> currentTargets)
    {
        List<Transform> toRemove = new List<Transform>();

        foreach (var kvp in activeIndicators)
        {
            if (!currentTargets.Contains(kvp.Key) || kvp.Key == null)
            {
                // CRITICAL FIX: Pass the type to the pool
                ReturnToPool(kvp.Value.gameObject, kvp.Value.type);
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var target in toRemove)
        {
            activeIndicators.Remove(target);
        }
    }

    /// <summary>
    /// Hides all indicators (when lock-on system is disabled by damage).
    /// </summary>
    private void HideAllIndicators()
    {
        foreach (var indicator in activeIndicators.Values)
        {
            if (indicator.gameObject != null)
            {
                indicator.gameObject.SetActive(false);
            }
        }
    }

    // ========================================================================
    // OBJECT POOLING
    // ========================================================================

    /// <summary>
    /// Gets an inactive indicator from pool.
    /// </summary>
    private GameObject GetFromPool(IndicatorType type)
    {
        // Check if a pool exists for this type and has objects
        if (indicatorPools.TryGetValue(type, out Stack<GameObject> pool) && pool.Count > 0)
        {
            GameObject obj = pool.Pop();
            obj.SetActive(true);
            return obj;
        }
        return null;
    }

    /// <summary>
    /// Returns indicator to pool for reuse.
    /// </summary>
    private void ReturnToPool(GameObject obj, IndicatorType type)
    {
        if (obj == null) return;

        obj.SetActive(false);

        // Initialize stack if it doesn't exist
        if (!indicatorPools.ContainsKey(type))
        {
            indicatorPools[type] = new Stack<GameObject>();
        }

        // Add to pool
        indicatorPools[type].Push(obj);
    }

    /// <summary>
    /// Gets the correct prefab for indicator type.
    /// </summary>
    private GameObject GetIndicatorPrefab(IndicatorType type)
    {
        switch (type)
        {
            case IndicatorType.Tracking:
                return TrackingIndicatorPrefab;
            case IndicatorType.Locked:
                return LockedIndicatorPrefab;
            case IndicatorType.Primary:
                return PrimaryTargetIndicatorPrefab;
            default:
                return null;
        }
    }

    // ========================================================================
    // PUBLIC METHODS
    // ========================================================================

    /// <summary>
    /// Manually enables/disables lock-on system (e.g., from external damage system).
    /// </summary>
    public void SetLockOnSystemActive(bool active)
    {
        lockOnSystemActive = active;
    }
}

// ============================================================================
// REQUIRED: ADD THESE METHODS TO YOUR WEAPONCONTROLLER.CS
// ============================================================================

/*
Add these public methods to your WeaponController class:

// ========================================================================
// PUBLIC GETTERS FOR UI SYSTEM
// ========================================================================

/// <summary>
/// Gets all currently tracked targets for UI display.
/// </summary>
public List<TargetTrackData> GetActiveTargets()
{
    return new List<TargetTrackData>(activeTargets);
}

/// <summary>
/// Gets primary targets (the ones that will be fired at).
/// </summary>
public List<TargetTrackData> GetPrimaryTargets()
{
    return new List<TargetTrackData>(primaryTargets);
}

// IMPORTANT: Make TargetTrackData public (currently private)
// Change this line in WeaponController:
// private class TargetTrackData  →  public class TargetTrackData
*/