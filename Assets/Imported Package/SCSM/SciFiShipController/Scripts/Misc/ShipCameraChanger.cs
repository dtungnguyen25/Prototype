using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sci-Fi Ship Controller. Copyright (c) 2018-2025 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    /// <summary>
    /// This component, which should be attached to a ShipControlModule (aka ship),
    /// can change the ShipCameraModule settings at runtime.
    /// </summary>
    [AddComponentMenu("Sci-Fi Ship Controller/Ship Components/Ship Camera Changer")]
    [HelpURL("https://scsmmedia.com/ssc-documentation")]
    public class ShipCameraChanger : MonoBehaviour
    {
        #region Public Variables

        /// <summary>
        /// If enabled, Initialise() will be called as soon as Start() runs. This should be disabled if you want to control when the component is enabled through code.
        /// </summary>
        public bool initialiseOnStart = false;

        /// <summary>
        /// Should the component be ready for use when it is first initialised?
        /// </summary>
        public bool isEnableOnInit = true;

        /// <summary>
        /// The camera settings to apply when the component is initialised. If the value is 0 or invalid, none will be applied.
        /// </summary>
        [Range(0,99)] public int cameraSettingOnInit = 0;

        /// <summary>
        /// If the settings are not applied when first initialised, which settings should be applied when cycled the first time?
        /// </summary>
        [Range(1, 99)] public int cameraSettingOnFirst = 1;

        /// <summary>
        /// When applying new settings, always snap the camera to the target location and rotation.
        /// This will override the SnapToTarget in the ShipCameraSettings scriptable object.
        /// </summary>
        public bool isAlwaysSnapToTarget = false;

        /// <summary>
        /// These methods are called immediately after the camera settings have been changed.
        /// Prev and curr Settings Index 1,2,3 etc. 0 = unset.
        /// </summary>
        public SSCCameraSettingsEvt1 onChangeCameraSettings = null;

        #endregion

        #region Public Properties

        /// <summary>
        /// The zero-based index of the currently used ShipCameraSetttings.
        /// Will return - 1 if the module is not initialised, or there are no active camera settings applied.
        /// </summary>
        public int CurrentCameraSettingsIndex { get { return isInitialised ? currentCameraSettingsIndex : -1; } }

        /// <summary>
        /// Get or set if the changer is ready for use. If not initialised, the changer will not be enabled.
        /// </summary>
        public bool IsChangerEnabled { get { return isInitialised && isChangerEnabled; } set { if (isInitialised) { isChangerEnabled = value; } else { isChangerEnabled = false; } } }

        /// <summary>
        /// Has the component been initialised?
        /// </summary>
        public bool IsInitialised { get { return isInitialised; } }

        #endregion

        #region Public Static Variables

        #endregion

        #region Protected Variables - Serialized

        /// <summary>
        /// The module used to control the player ship camera
        /// </summary>
        [SerializeField] protected ShipCameraModule shipCameraModule = null;

        /// <summary>
        /// A list of ShipCameraSettings for switching between camera settings at runtime.
        /// </summary>
        [SerializeField] protected List<ShipCameraSettings> shipCameraSettingsList = new List<ShipCameraSettings>();

        #endregion

        #region Protected Variables - General

        /// <summary>
        /// Is the component ready for use?
        /// </summary>
        protected bool isChangerEnabled = false;

        protected bool isInitialised = false;

        protected int currentCameraSettingsIndex = -1;
        protected int currentCameraSettingsHash = 0;

        protected int numCamSettings = 0;

        protected bool isFirstTimeCycle = true;

        protected ShipControlModule shipControlModule = null;

        #endregion

        #region Protected and Public Variables - Editor

        [SerializeField] protected int selectedTabInt = 0;
        [HideInInspector] public bool allowRepaint = false;

        #endregion

        #region Public Delegates

        #endregion

        #region Private Initialise Methods

        // Use this for initialization
        void Start()
        {
            if (initialiseOnStart) { Initialise(); }
        }

        #endregion

        #region Protected and Internal Methods - General

        /// <summary>
        /// Attempt to apply camera settings
        /// </summary>
        /// <param name="camSettings"></param>
        /// <param name="prevCameraSettingsIndex"></param>
        protected void ApplyCameraSettings(ShipCameraSettings camSettings, int prevCamSettingsIndex, int currCamSettingsIndex)
        {
            // Make sure we're not just trying to re-apply the same settings
            if (isChangerEnabled && camSettings != null && camSettings.GetHashCode() != currentCameraSettingsHash)
            {
                currentCameraSettingsHash = camSettings.GetHashCode();
                currentCameraSettingsIndex = currCamSettingsIndex;
                shipCameraModule.ApplyCameraSettings(camSettings);

                int shipId = shipControlModule.GetShipId;

                if (isAlwaysSnapToTarget && !camSettings.isSnapToTarget) { shipCameraModule.SnapToTarget(); }

                // Event prev and curr Settings Index 1,2,3 etc. 0 = unset.
                if (onChangeCameraSettings != null) { onChangeCameraSettings.Invoke(prevCamSettingsIndex + 1, currCamSettingsIndex + 1, shipId, false); }
            }
        }

        /// <summary>
        /// Attempt to get the next zero-based index in the list of ship camera set.
        /// NOTE: The slot might not contain a valid ShipCameraSettings ScriptableObject.
        /// </summary>
        /// <returns></returns>
        protected int GetNextCameraSettingsIndex()
        {
            int nextSettingIndex = -1;

            if (isInitialised)
            {
                int numSettingSlots = shipCameraSettingsList.Count;

                if (numSettingSlots > 0)
                {
                    // If not set yet, start in the first slot
                    if (currentCameraSettingsIndex < 0) { nextSettingIndex = 0; }
                    else
                    {
                        nextSettingIndex = (currentCameraSettingsIndex + 1) % numSettingSlots;
                    }
                }
            }

            return nextSettingIndex;
        }

        #endregion

        #region Events

        #endregion

        #region Public API Methods - General

        /// <summary>
        /// Attempt to add a ShipCameraSettings scriptable object to the list on the General tab.
        /// </summary>
        /// <param name="cameraSettings"></param>
        public void AddCameraSetting (ShipCameraSettings cameraSettings)
        {
            if (cameraSettings != null)
            {
                shipCameraSettingsList.Add(cameraSettings);

                numCamSettings = shipCameraSettingsList.Count;
            }
        }

        /// <summary>
        /// Attempt to cycle through a list of camera settings.
        /// It will skip over any empty settings slots and will
        /// not apply the same settings twice in a row.
        /// </summary>
        public void CycleCameraSettings()
        {
            if (isInitialised && isChangerEnabled && shipCameraModule != null)
            {
                if (isFirstTimeCycle)
                {
                    isFirstTimeCycle = false;
                    // Only apply first time settings if they weren't set when
                    // first initialised.
                    if (cameraSettingOnInit == 0)
                    {
                        SelectCameraSettings(cameraSettingOnFirst);
                        return;
                    }
                }

                int numCamSettings = shipCameraSettingsList.Count;

                int prevCameraSettingsIndex = currentCameraSettingsIndex;

                // Transverse the list of settings a maximum of once
                for (int iterations = 0; iterations < numCamSettings; iterations++)
                {
                    int settingIdx = GetNextCameraSettingsIndex();

                    // Is the index valid for the list of camera settings?
                    if (settingIdx >= 0 && settingIdx < numCamSettings)
                    {
                        currentCameraSettingsIndex = settingIdx;

                        ShipCameraSettings camSettings = shipCameraSettingsList[settingIdx];

                        if (camSettings != null)
                        {
                            ApplyCameraSettings(camSettings, prevCameraSettingsIndex, currentCameraSettingsIndex);
                            break;
                        }
                        else
                        {
                            // This settings slot is empty, so look for another one
                            continue;
                        }
                    }
                    else
                    {
                        // No valid setting index found, so exit the loop
                        break;
                    }
                }

                #if UNITY_EDITOR
                if (numCamSettings == 0)
                {
                    Debug.LogWarning("ShipCameraChanger.CycleCameraSettings() - No Camera Settings found on the General tab");
                }
                #endif
            }
        }

        /// <summary>
        /// Get the current ship camera module (if any)
        /// </summary>
        /// <returns></returns>
        public ShipCameraModule GetShipCameraModule()
        {
            return shipCameraModule;
        }

        /// <summary>
        /// Attempt to initialise the component and make it ready for use
        /// </summary>
        public void Initialise()
        {
            if (isInitialised) { return; }

            if (shipCameraModule == null)
            {
                Debug.LogWarning("[ERROR] ShipCameraChanger on " + name + " cannot be initialised without a ShipCameraModule");
            }
            else if (!TryGetComponent(out shipControlModule))
            {
                Debug.LogWarning("[ERROR] ShipCameraChanger on " + name + " cannot find the ShipControlModule. This component needs to be attached to a ship.");
            }
            else
            {
                numCamSettings = shipCameraSettingsList.Count;

                isChangerEnabled = isEnableOnInit;

                isInitialised = true;

                if (cameraSettingOnInit > 0) { SelectCameraSettings(cameraSettingOnInit); }
            }
        }

        /// <summary>
        /// Attempt to remove a camera setting at a particular position in the list
        /// </summary>
        /// <param name="settingsNumber"></param>
        public void RemoveCameraSetting (int settingsNumber)
        {
            numCamSettings = shipCameraSettingsList.Count;

            if (settingsNumber > 0 && settingsNumber <= numCamSettings)
            {
                // If removing the current position clear it.
                if (currentCameraSettingsIndex == settingsNumber - 1)
                {
                    currentCameraSettingsIndex = -1;
                    currentCameraSettingsHash = 0;
                }

                shipCameraSettingsList.RemoveAt(settingsNumber - 1);
            }
        }

        /// <summary>
        /// Attempt to select and apply camera settings from the list in the General tab.
        /// Numbers start at 1.
        /// </summary>
        /// <param name="settingsNumber">Numbers start at 1</param>
        public void SelectCameraSettings (int settingsNumber)
        {
            if (!isInitialised)
            {
                Debug.LogWarning("[ERROR] ShipCameraChanger.SelectCameraSettings() - the component has not been initialised");
            }
            else if (settingsNumber > 0 && settingsNumber <= numCamSettings)
            {
                ApplyCameraSettings(shipCameraSettingsList[settingsNumber - 1], currentCameraSettingsIndex, settingsNumber-1);
            }
            else if (numCamSettings == 0)
            {
                Debug.LogWarning("[ERROR] ShipCameraChanger.SelectCameraSettings() - there are no camera settings in the list on the General tab");
            }
            else
            {
                Debug.LogWarning("[ERROR] ShipCameraChanger.SelectCameraSettings() - the settingsNumber (" + settingsNumber + ") must in the range " + 1 + " to " + numCamSettings);
            }
        }

        /// <summary>
        /// Attempt to set the ship camera module used for applying the camera settings
        /// </summary>
        /// <param name="newShipCameraModule"></param>
        public void SetShipCameraModule (ShipCameraModule newShipCameraModule)
        {
            shipCameraModule = newShipCameraModule;
        }

        #endregion

        #region Public API Methods - Events

        /// <summary>
        /// Call this when you wish to remove any custom event listeners, like
        /// after creating them in code and then destroying the object.
        /// You could add this to your game play OnDestroy code.
        /// </summary>
        public virtual void RemoveListeners()
        {
            if (isInitialised)
            {
                if (onChangeCameraSettings != null) { onChangeCameraSettings.RemoveAllListeners(); }
            }
        }

        #endregion

        #region Public API Methods - Orbit Camera

        /// <summary>
        /// Send orbit horizontal input to the camera. It is ignored if Orbit is not enabled.
        /// Values should be between -1.0 and 1.0.
        /// Orbit right for +ve values. Orbit left for -ve values.
        /// The value is automatically reset to 0 after use.
        /// </summary>
        /// <param name="orbitInput"></param>
        public void SendOrbitHorizInput (float orbitInput)
        {
            if (shipCameraModule != null) { shipCameraModule.SendOrbitHorizInput(orbitInput); }
        }

        /// <summary>
        /// Send orbit vertical input to the camera. It is ignored if Orbit is not enabled.
        /// Values should be between -1.0 and 1.0.
        /// Orbit up for +ve values. Orbit down for -ve values.
        /// The value is automatically reset to 0 after use.
        /// </summary>
        /// <param name="orbitInput"></param>
        public void SendOrbitVertInput (float orbitInput)
        {
            if (shipCameraModule != null) { shipCameraModule.SendOrbitVertInput(orbitInput); }
        }

        /// <summary>
        /// Make the orbit camera feature available for use or unavailable.
        /// </summary>
        /// <param name="isAvailable"></param>
        public void SetIsOrbitAvailable (bool isAvailable)
        {
            if (shipCameraModule != null) { shipCameraModule.SetIsOrbitAvailable(isAvailable); }
        }

        /// <summary>
        /// The amount of orbit to apply.
        /// x = local horizontal (rotate around local y axis).
        /// y = local vertical (rotate around local x axis).
        /// 0,0 is no orbit.
        /// 1.0 is fully orbited right or pitched up.
        /// -1.0 is fully orbited left or pitched down.
        /// </summary>
        /// <param name="orbitValue"></param>
        public void SetOrbitAmount (Vector2 orbitValue)
        {
            if (shipCameraModule != null) { shipCameraModule.SetOrbitAmount(orbitValue); }
        }

        /// <summary>
        /// Attempt to toggle orbit camera on or off.
        /// </summary>
        public void ToggleOrbitCamera()
        {
            if (shipCameraModule != null) { shipCameraModule.ToggleOrbit(); }
        }

        #endregion
    }
}