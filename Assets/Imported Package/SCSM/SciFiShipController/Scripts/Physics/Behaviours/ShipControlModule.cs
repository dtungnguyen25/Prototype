using System.Collections.Generic;
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Sci-Fi Ship Controller. Copyright (c) 2018-2025 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    [AddComponentMenu("Sci-Fi Ship Controller/Ship Components/Ship Control Module")]
    [HelpURL("https://scsmmedia.com/ssc-documentation")]
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class ShipControlModule : MonoBehaviour
    {
        #region Public Variables

        [HideInInspector] public bool allowRepaint = false;

        /// <summary>
        /// The class in which the majority of ship data is stored in. Typically use this to access/modify data relating to a ship.
        /// You should check IsInitialised before referencing the shipInstance.
        /// </summary>
        public Ship shipInstance;

        /// <summary>
        /// [INTERNAL USE ONLY]
        /// </summary>
        public GameObject[] thrusterEffectObjects = new GameObject[1];

        // Remember which tabs etc were shown
        [HideInInspector] public int selectedTabInt = 0;

        /// <summary>
        /// [EDITOR USE ONLY]
        /// </summary>
        [Range(0.001f, 50f)] public float gizmoScale = 1f;

        /// <summary>
        /// [INTERNAL USE ONLY]
        /// </summary>
        public string editorSearchThrusterFilter;

        #endregion

        #region Public Properties

        /// <summary>
        /// {READONLY] Does the ship have a Ship AI Input Module attached?
        /// </summary>
        public bool HasShipAIInputModule { get => isInitialised ? isShipAIInputModuleAttached && shipAIInputModule != null : isShipAIInputModuleAttached = TryGetComponent(out shipAIInputModule); }

        /// <summary>
        /// [READONLY] Has the ship been initialised?
        /// </summary>
        public bool IsInitialised { get { return isInitialised; } }

        /// <summary>
        /// Attempt to get or set the faction or alliance the ship belongs to (ship must be initialised). This can be used to identify if a ship is friend or foe.  Neutral = 0.
        /// </summary>
        public int FactionId { get { return isInitialised ? shipInstance.factionId : 0; } set { if (isInitialised) { shipInstance.factionId = value; } } }

        /// <summary>
        /// [READONLY] Session-only transform InstanceID.
        /// </summary>
        public int GetShipId { get { return isInitialised ? shipInstance.shipId : transform.GetInstanceID(); } }

        /// <summary>
        /// Attempt to get or set gravitational acceleration (must be initialised)
        /// </summary>
        public float GravitationalAcceleration { get { return isInitialised ? shipInstance.gravitationalAcceleration : 0f; } set { if (isInitialised) { shipInstance.gravitationalAcceleration = value; } } }

        /// <summary>
        /// Attempt to get or set the direction gravity is acting the ship (must be initialised)
        /// </summary>
        public Vector3 GravityDirection { get { return isInitialised ? shipInstance.gravityDirection : Vector3.zero; } set { if (isInitialised) { shipInstance.gravityDirection = value; } } }

        /// <summary>
        /// [READONLY] The rigidbody of the ship.
        /// </summary>
        public Rigidbody ShipRigidbody { get { return rBody; } }

        /// <summary>
        /// Are the thruster systems online or in the process of coming online?
        /// At runtime call StartupThrusterSystems() or ShutdownThrusterSystems().
        /// </summary>
        public bool IsThrusterSystemsStarted { get { return isInitialised && shipInstance.isThrusterSystemsStarted; } }

        /// <summary>
        /// [READONLY] Is the ship currently being respawned?
        /// The ship will be disabled during the respawn time.
        /// </summary>
        public bool IsRespawning { get { return isRespawning; } }

        /// <summary>
        /// [READONLY] Has respawning been paused? If so, this ship cannot respawn
        /// until ResumeRespawning() has been called. See also PauseRespawning().
        /// </summary>
        public bool IsRespawningPaused { get { return isRespawingPaused; } }

        /// <summary>
        /// [READONLY] Is this ship visible to the radar?
        /// </summary>
        public bool IsVisbleToRadar { get { return isInitialised && shipInstance != null && shipInstance.isRadarEnabled && sscRadar != null && sscRadar.GetVisibility(shipInstance.radarItemIndex);  } }

        /// <summary>
        /// Attempt to get or set the model ID of this ship (ship must be initialised).
        /// </summary>
        public int ModelId { get { return isInitialised ? shipInstance.modelId : 0; } set { if (isInitialised) { shipInstance.modelId = value; } } }

        /// <summary>
        /// [READONLY] The number of thrusters on this ship.
        /// </summary>
        public int NumberOfThrusters { get { return isInitialised ? shipInstance.thrusterList == null ? 0 : shipInstance.thrusterList.Count : shipInstance == null ? 0 : shipInstance.thrusterList == null ? 0 : shipInstance.thrusterList.Count; } }

        /// <summary>
        /// [READONLY] The number of weapons on this ship. Will always return 0 if ship has not been initialised.
        /// See also ship.NumberOfWeapons.
        /// </summary>
        public int NumberOfWeapons { get { return isInitialised ? (shipInstance.weaponList == null ? 0 : shipInstance.weaponList.Count) : 0; } }

        /// <summary>
        /// [READONLY] The number of times the ship has been respawned. This is incremented
        /// when the ship is respawned - not when it is destroyed.
        /// </summary>
        public int NumberOfRespawns { get { return respawnCount; } }

        /// <summary>
        /// Attempt to get or set the (unique) squadron this ship is a member of (ship must be initialised). Do not place friendly and enemy ships in the same squadron.
        /// </summary>
        public int SquadronId { get { return isInitialised ? shipInstance.squadronId : 0; } set { if (isInitialised) { shipInstance.squadronId = value; } } }

        #if UNITY_EDITOR

        public string ThrusterSystemsStatus { get { return isInitialised ? (shipInstance.isThrusterSystemsStarted ? (shipInstance.thrusterSystemShutdownTimer > 0f ? "shutting down" : (shipInstance.thrusterSystemStartupTimer > 0f ? "starting up" : "online") ) : "offline") : "--"; } }

        #endif

        #endregion

        #region Private Variables

        private bool isInitialised = false;
        private SSCManager sscManager;
        private SSCRadar sscRadar;

        private ThrusterEffects[] thrusterEffects;

        /// <summary>
        /// Used internally to reset PID Controllers on AI ships
        /// when ship velocity is set to 0.
        /// </summary>
        private ShipAIInputModule shipAIInputModule;
        private bool isShipAIInputModuleAttached = false;

        private ShipDocking shipDocking;
        private bool isShipDockingAttached = false;

        private Rigidbody rBody;
        private List<Renderer> activeRenderersList;
        private List<AudioSource> audioSourcesList;
        /// <summary>
        /// This prevents the activeRenderersList being updated while the ship
        /// is disabled
        /// </summary>
        private bool isShipVisibilityDisabled = false;

        private int componentIndex;
        private int arrayLength;
        private Thruster thruster;

        private bool shipIsEnabled = true;

        /// <summary>
        /// This is a subset of shipIsEnabled. Current applies to:
        /// 1) Physics
        /// 2) Thruster Effects
        /// 3) User or AI Input
        /// 4) Sound FX (audio)
        /// </summary>
        private bool isMovementEnabled = true;

        private int lastDamageEventIndex = 0;
        private float damageRumbleTimer = -1f;

        #region Input Variables

        private Vector3 pilotForceInput = Vector3.zero;
        // Make non-serialized internal so is visible to ShipCameraModule
        [System.NonSerialized] internal Vector3 pilotMomentInput = Vector3.zero;
        // TODO: There could be a problem with ship fire input
        // If framerate drops below 50 FPS, fixedupdate will be called more often than update
        // Therefore it would be possible for fixedupate to be called twice or more between update calls
        // So if fire can be held is false, fire could still be true for multiple frames for a single fire call
        // However, maybe this won't be a problem if reload time is kept high enough?
        private bool pilotPrimaryFireInput = false;
        private bool pilotSecondaryFireInput = false;
        private bool pilotDockInput = false;

        #endregion

        #region FixedUpdate Variables

        protected Vector3 localResultantForce;
        protected Vector3 localResultantMoment;

        private bool isRespawning = false;
        private bool isRespawingPaused = false;
        private int respawnCount = 0;
        private float respawnTimer = 0f;
        private float stuckTimer = 0f;

        #endregion

        #endregion

        #region Internal Variables

        [System.NonSerialized] internal int sceneHandle = 0;

        /// <summary>
        /// Is the ship being moved between scenes using the JumpManager
        /// from SSC Seamless (aka XPack2)?
        /// </summary>
        [System.NonSerialized] internal bool isTransitioning = false;

        #endregion

        #region Public Static Version Properties

        public static string SSCVersion { get { return "1.5.4"; } }
        public static string SSCBetaVersion { get { return ""; } }

        #endregion

        #region Public Delegates

        public delegate void CallbackOnCollision(ShipControlModule shipControlModule, Collision collision);
        public delegate void CallbackOnClearManager();
        public delegate void CallbackOnDestroy(Ship ship);
        public delegate void CallbackOnRespawn(ShipControlModule shipControlModule, ShipAIInputModule shipAIInputModule);
        public delegate void CallbackOnHit(CallbackOnShipHitParameters callbackOnShipHitParameters);
        public delegate void CallbackOnThrusterSystemsStatusChanged(ShipControlModule shipControlModule, int newStatus);
        public delegate void CallbackOnStuck(ShipControlModule shipControlModule);
        public delegate void CallbackOnRefreshManager(SSCManager sscManager);
        public delegate void CallbackOnRumble(float rumbleAmount);
        public delegate void CallbackOnPhysicsLoop(float deltaTime);
        public delegate void CallbackOnUpdateLoop(float deltaTime);

        /// <summary>
        /// Subscribe with a custom method to this to be notified immediately after a collision.
        /// Your method must take 2 parameters (ShipControlModule and Collision).
        /// This should be a lightweight method to avoid performance issues.
        /// </summary>
        [System.NonSerialized] public CallbackOnCollision callbackOnCollision = null;

        /// <summary>
        /// Subscribe with a custom method to this to be notified when the SSCManager is being
        /// cleared. This is typically called when moving a ship out of a scene using SSC Seamless.
        /// </summary>
        [System.NonSerialized] public CallbackOnClearManager callbackOnClearManager = null;

        /// <summary>
        /// Subscribe with a custom method to this to be notified immediately
        /// before the ship is destroyed. Your method must take 1
        /// parameter of class Ship. This should be a lightweight
        /// method to avoid performance issues. It could be used to update
        /// a score or remove a ship from a squadron.
        /// </summary>
        [System.NonSerialized] public CallbackOnDestroy callbackOnDestroy = null;

        /// <summary>
        /// Subscribe with a custom method to this to be notified immediately
        /// after the ship is respawned. Your method must take 2 parameters: ShipControlModule
        /// and ShipAIInputModule. The first is never null but there may be no AI module
        /// attached to this ship. This should be a lightweight method to avoid
        /// performance issues.
        /// </summary>
        [System.NonSerialized] public CallbackOnRespawn callbackOnRespawn = null;

        /// <summary>
        /// Subscribe with a custom method to this to be notified immediately
        /// after the ship is hit by a projectile or beam. Your method must take 1
        /// parameter of type CallbackOnShipHitParameters. This should be 
        /// a lightweight method to avoid performance issues. It could be used to 
        /// take evasive action while being pursued by an enemy ship. It could
        /// also be used to detect friendly fire.
        /// </summary>
        [System.NonSerialized] public CallbackOnHit callbackOnHit = null;

        /// <summary>
        /// Subscribe with a custom method to this to be notified immediately after
        /// the thruster systems status has changed. Your method must take
        /// 2 parameters: ShipControlModule and int.
        /// Status 0 = Starting, 1 = Started, 2 = Shutting down, 3 = Shutdown.
        /// This should be a lightweight method to avoid performance issues
        /// and should not keep a reference to shipControlModule.
        /// </summary>
        [System.NonSerialized] public CallbackOnThrusterSystemsStatusChanged callbackOnThrusterSystemsChanged = null;

        /// <summary>
        /// Subscribe with a custom method to this to be notified immediately after
        /// a ship is detected as stuck. To avoid performance issues, action
        /// should be taken otherwise your method may be called each subsequent
        /// frame. See also ship.stuckTime and ship.stuckSpeedThreshold.
        /// </summary>
        [System.NonSerialized] public CallbackOnStuck callbackOnStuck = null;

        /// <summary>
        /// Subscribe with a custom method to this to be notified when the SSCManager is being
        /// refreshed. This is typically called when a ship has been moved to a different scene
        /// using SSC Seamless (aka XPack2).
        /// </summary>
        [System.NonSerialized] public CallbackOnRefreshManager callbackOnRefreshManager = null;

        /// <summary>
        /// Generally reserved for internal use by the PlayerInputModule. If the human
        /// player ship does not have the PlayerInputModule component attached, you can
        /// create a lightweight custom method and assign it at runtime so that it is
        /// called whenever device rumble or force feedback is required.
        /// </summary>
        [System.NonSerialized] public CallbackOnRumble callbackOnRumble = null;

        /// <summary>
        /// Subscribe to this to get notified when TimingPhysicsLoop is manual
        /// and PhysicsSimulation(deltaTime) has been called.
        /// See also ship.callbackOnTimingPhysicsChanged.
        /// </summary>
        [System.NonSerialized] public CallbackOnPhysicsLoop callbackOnPhysicsLoop = null;

        /// <summary>
        /// Subscribe to this to get notified when TimingUpdateLoop is manual
        /// and UpdateLoop(deltaTime) has been called.
        /// See also ship.callbackOnTimingUpdateChanged
        /// </summary>
        [System.NonSerialized] public CallbackOnUpdateLoop callbackOnUpdateLoop = null;

        #endregion

        #region Private Initialise Methods

        // Use this for initialization
        void Awake()
        {
            if (shipInstance.initialiseOnAwake)
            {
                InitialiseShip();
            }
        }

        #endregion

        #region Update Methods

        private void Update()
        {
            if (isInitialised && shipInstance.timingUpdateLoopInt == Ship.SSCTimingAutoInt)
            {
                UpdateLoop(Time.deltaTime);                
            }
        }

        // FixedUpdate is called once per physics update (typically about 50 times per second)
        private void FixedUpdate()
        {
            if (isInitialised && shipInstance.timingPhysicsLoopInt == Ship.SSCTimingAutoInt)
            {
                PhysicsSimulation(Time.deltaTime);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Check if we need to respawn the ship. Moved to FixedUpdate() to support U2022.x
        /// </summary>
        /// <param name="isFixedUpdate"></param>
        private void CheckRespawnShip(bool isFixedUpdate)
        {
            // Once respawn time reaches zero, respawn the ship
            if (respawnTimer <= 0f)
            {
                // We are now not in the process of respawning
                isRespawning = false;
                respawnCount++;
                // Reset the health of the ship
                shipInstance.ResetHealth();
                shipInstance.ResetThrusterSystems();

                if (shipInstance.respawningMode == Ship.RespawningMode.RespawnOnPath)
                {
                    RespawnOnPath(shipInstance.respawningPathGUIDHash, true, false, isFixedUpdate);
                }
                else
                {
                    Vector3 respawnPos = shipInstance.GetRespawnPosition();
                    Quaternion respawnRot = shipInstance.GetRespawnRotation();

                    if (isFixedUpdate && rBody.isKinematic)
                    {
                        // Move the rigidbody (required for U2022+)
                        rBody.MovePosition(respawnPos);
                        rBody.MoveRotation(respawnRot);

                        // Also update the transform in the same frame
                        // Set ship position and rotation
                        transform.SetPositionAndRotation(respawnPos, respawnRot);
                    }
                    else
                    {
                        // Set ship position and rotation
                        transform.SetPositionAndRotation(respawnPos, respawnRot);
                    }

                    // Re-enable the ship and make it visible again
                    EnableOrDisableShip(true, true, false);
                }

                // Re-initialise ground match variables since this will also reset PID controller
                shipInstance.ReinitialiseGroundMatchVariables();

                // Set ship velocity
                #if UNITY_6000_0_OR_NEWER
                rBody.linearVelocity = transform.TransformDirection(shipInstance.respawnVelocity);
                #else
                rBody.velocity = transform.TransformDirection(shipInstance.respawnVelocity);
                #endif
                rBody.angularVelocity = Vector3.zero;

                // If there is an AI component attached, reset the PID controllers
                // TODO: Test whether this is effective for when an AI respawns with a given velocity
                if (isShipAIInputModuleAttached && shipAIInputModule != null)
                {
                    if (shipAIInputModule.IsInitialised) { shipAIInputModule.ResetPIDControllers(); }
                    else { shipAIInputModule.Initialise(); }
                }

                if (callbackOnRespawn != null) { callbackOnRespawn(this, shipAIInputModule); }
            }
        }

        /// <summary>
        /// Enables or disables the ship (depending on the value of isEnabled). If updateVisibility is set to true, 
        /// changes whether the ship is visible. If resetVelocity is set to true, resets the velocity of the ship to zero.
        /// Movement is not enabled if the ship is docked, although collision detection is updated.
        /// </summary>
        /// <param name="isEnabled"></param>
        /// <param name="updateVisibility"></param>
        /// <param name="resetVelocity"></param>
        private void EnableOrDisableShip(bool isEnabled, bool updateVisibility, bool resetVelocity)
        {
            // This does not prove the ship is docked, but the ship thinks it is docked. For absolute proof, we would
            // need to check the ShipDockingStation's docking points (which is prob too expensive here).
            bool isDocked = isShipDockingAttached && shipDocking.IsInitialised && shipDocking.GetStateInt() == ShipDocking.dockedInt;

            if (isDocked && isEnabled)
            {
                ShipRigidbody.detectCollisions = shipDocking.detectCollisionsWhenDocked;
            }
            else
            {
                EnableOrDisableShipPhysics(isEnabled, resetVelocity);
                // Pause/resume thruster effects
                PauseOrResumeThrusters(isEnabled && isInitialised && shipInstance.isThrusterSystemsStarted);
            }

            #region Update Visuals

            if (updateVisibility)
            {
                if (isEnabled)
                {
                    // Re-enable previously active renderers
                    arrayLength = activeRenderersList.Count;
                    for (componentIndex = 0; componentIndex < arrayLength; componentIndex++)
                    {
                        activeRenderersList[componentIndex].enabled = true;
                    }
                    isShipVisibilityDisabled = false;
                }
                else
                {
                    shipInstance.DeactivateBeams(sscManager);

                    // We are disabling the ship - so we disable all visual and audial components

                    #region Find and Disable Renderers

                    if (!isShipVisibilityDisabled)
                    {
                        // Prevent the list being cleared when the ship is already disabled
                        isShipVisibilityDisabled = true;

                        // Get all the renderers in the ship and store them in a list
                        // Use a pre-defined reusable list. If respawning happens multiple
                        // times there will be a lot of GC which will hurt performance.
                        GetComponentsInChildren(activeRenderersList);

                        // Loop through the renderers
                        arrayLength = activeRenderersList.Count;
                        componentIndex = 0;
                        while (componentIndex < arrayLength)
                        {
                            if (!activeRenderersList[componentIndex].enabled)
                            {
                                // If the renderer is currently disabled, remove it from the list
                                // (We don't want to enable it when we respawn)
                                activeRenderersList.RemoveAt(componentIndex);
                                arrayLength--;
                            }
                            else
                            {
                                // If the renderer is currently enabled, disable it (and leave it in the list)
                                activeRenderersList[componentIndex].enabled = false;
                                componentIndex++;
                            }
                        }
                    }
                    #endregion

                    StopAudioSources();
                }
            }

            #endregion

            #region Update Rumble

            if (!isEnabled && callbackOnRumble != null) { callbackOnRumble(0f); }

            #endregion

            #region Update Radar
            if (shipInstance.isRadarEnabled)
            {
                sscRadar.SetVisibility(shipInstance.RadarId, isEnabled);

                // If any localised damage regions have radar enabled, set the visibility.
                if (shipInstance.shipDamageModel == Ship.ShipDamageModel.Localised)
                {
                    for (int dmIdx = 0; dmIdx < shipInstance.numLocalisedDamageRegions; dmIdx++)
                    {
                        DamageRegion damageRegion = shipInstance.localisedDamageRegionList[dmIdx];
                        if (damageRegion != null && damageRegion.isRadarEnabled)
                        {
                            sscRadar.SetVisibility(damageRegion.radarItemIndex, isEnabled);
                        }
                    }
                }

                // Added for TechDemo3 (SSC v1.2.6 Beta 1b)
                if (isEnabled && isInitialised)
                {
                    shipInstance.UpdatePositionAndMovementData(transform, rBody);

                    // Update fields that may change at any time
                    shipInstance.sscRadarPacket.isVisibleToRadar = true;
                    shipInstance.sscRadarPacket.position = shipInstance.TransformPosition;
                    shipInstance.sscRadarPacket.velocity = shipInstance.WorldVelocity;
                    shipInstance.sscRadarPacket.factionId = shipInstance.factionId;
                    shipInstance.sscRadarPacket.squadronId = shipInstance.squadronId;

                    // Use the internal radarItemIndex rather than RadarId to avoid the property lookup
                    sscRadar.UpdateItem(shipInstance.radarItemIndex, shipInstance.sscRadarPacket);
                }
            }

            #endregion

            // Update the shipIsEnabled variable
            shipIsEnabled = isEnabled;

            // Movement is a subset of the ship being enabled/disabled.
            isMovementEnabled = isEnabled && !isDocked;
        }

        /// <summary>
        /// This is a subset of EnableOrDisableShip(...). The ship can still incur damage,
        /// be destroyed and respawn (and become Enabled), and appear on Radar.
        /// It applies to:
        /// 1) Physics
        /// 2) Thruster Effects
        /// 3) User or AI Input to the Ship
        /// 4) Sound FX (audio)
        /// Movement is not enabled if the ship is docked, although collision detection is updated.
        /// </summary>
        /// <param name="isEnabled"></param>
        /// <param name="resetVelocity"></param>
        private void EnableOrDisableShipMovement(bool isEnabled, bool resetVelocity)
        {
            // This does not prove the ship is docked, but the ship thinks it is docked. For absolute proof, we would
            // need to check the ShipDockingStation's docking points (which is prob too expensive here).
            bool isDocked = isShipDockingAttached && shipDocking.IsInitialised && shipDocking.GetStateInt() == ShipDocking.dockedInt;

            if (isDocked && isEnabled)
            {
                ShipRigidbody.detectCollisions = shipDocking.detectCollisionsWhenDocked;
            }
            else
            {
                EnableOrDisableShipPhysics(isEnabled, resetVelocity);
                PauseOrResumeThrusters(isEnabled && isInitialised && shipInstance.isThrusterSystemsStarted);
                if (!isEnabled && callbackOnRumble != null) { callbackOnRumble(0f); }
                if (!isEnabled) { StopAudioSources(); }

                isMovementEnabled = isEnabled;
            }
        }

        /// <summary>
        /// Enable or disable the Physics on a ship. If this ia an AI ship, resets the PID controllers
        /// when isEnabled is true.
        /// If resetVelocity and isEnabled are true, resets the velocity of the ship to zero.
        /// NOTE: This should NOT be called when the ship is docked.
        /// </summary>
        /// <param name="isEnabled"></param>
        /// <param name="resetVelocity"></param>
        private void EnableOrDisableShipPhysics(bool isEnabled, bool resetVelocity)
        {
            // Ensure the rigidbody's collision detection mode is configured to permit isKinematic.
            if (!isEnabled) { ShipRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete; }

            if (isEnabled)
            {
                // When enabling, turn on collisions and set the ship rigidbody to not kinematic
                ShipRigidbody.detectCollisions = true;
                ShipRigidbody.isKinematic = false;

                // Re-configure the rigidbody's collision detection mode
                ShipRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
            else
            {
                // When disabling, turn off collisions and set the ship rigidbody to kinematic
                ShipRigidbody.detectCollisions = false;
                ShipRigidbody.isKinematic = true;

                // Added 1.3.7 Beta 5c so that cached velo data correctly returns Vector.Zero
                // NOTE: This hasn't been fully regression tested.
                if (resetVelocity) { shipInstance.ResetVelocityData(); }
            }

            // Set our velocity when we enable the ship
            if (isEnabled)
            {
                if (resetVelocity)
                {
                    // Reset velocity to zero
                    #if UNITY_6000_0_OR_NEWER
                    ShipRigidbody.linearVelocity = Vector3.zero;
                    #else
                    ShipRigidbody.velocity = Vector3.zero;
                    #endif
                    ShipRigidbody.angularVelocity = Vector3.zero;

                    // If there is an AI component attached, reset the PID controllers
                    if (isShipAIInputModuleAttached && shipAIInputModule != null)
                    {
                        if (shipAIInputModule.IsInitialised) { shipAIInputModule.ResetPIDControllers(); }
                        else { shipAIInputModule.Initialise(); }
                    }
                }
                else
                {
                    // Set velocity to the velocity we had before the ship was disabled
                    // NOTE: This assumes shipInstance.ResetVelocityData() isn't called when this method
                    // is called with isEnabled = true.
                    #if UNITY_6000_0_OR_NEWER
                    ShipRigidbody.linearVelocity = shipInstance.WorldVelocity;
                    #else
                    ShipRigidbody.velocity = shipInstance.WorldVelocity;
                    #endif
                    ShipRigidbody.angularVelocity = shipInstance.WorldAngularVelocity;
                }
            }
        }

        /// <summary>
        /// Pause or Resume (play) all Thruster Effects on the ship
        /// </summary>
        /// <param name="isEnabled">Resume = true, Pause = false</param>
        internal void PauseOrResumeThrusters(bool isEnabled)
        {
            arrayLength = thrusterEffects == null ? 0 : thrusterEffects.Length;
            for (componentIndex = 0; componentIndex < arrayLength; componentIndex++)
            {
                if (thrusterEffects[componentIndex] != null)
                {
                    if (isEnabled) { thrusterEffects[componentIndex].Play(); }
                    else { thrusterEffects[componentIndex].Pause(); }
                }
            }
        }

        /// <summary>
        /// Find and stop all audio sources
        /// </summary>
        private void StopAudioSources()
        {
            // Use the pre-defined reuseable list to minmize GC
            GetComponentsInChildren(audioSourcesList);
            arrayLength = audioSourcesList.Count;
            for (componentIndex = 0; componentIndex < arrayLength; componentIndex++)
            {
                if (audioSourcesList[componentIndex].enabled && audioSourcesList[componentIndex].isPlaying)
                {
                    audioSourcesList[componentIndex].Stop();
                }
            }
        }

        /// <summary>
        /// CURRENTLY NOT USED - FOR TESTING ONLY
        /// </summary>
        /// <returns></returns>
        private IEnumerator RBodyReset()
        {
            //rBody.velocity = transform.TransformDirection(shipInstance.respawnVelocity);
            #if UNITY_6000_0_OR_NEWER
            rBody.linearVelocity = Vector3.zero;
            #else
            rBody.velocity = Vector3.zero;
            #endif
            rBody.angularVelocity = Vector3.zero;

            yield return new WaitForFixedUpdate();
        }

        /// <summary>
        /// A ship can be considered stuck if it is enabled, initialised, respawnStuckTime > 0
        /// and it is trying to move for respawnStuckTime but cannot.
        /// </summary>
        /// <returns></returns>
        private bool IsShipStuck(float deltaTime)
        {
            if (!shipIsEnabled || !isInitialised || shipInstance.stuckTime <= 0f) { return false; }
            else
            {
                // velo in m/s
                if (Mathf.Abs((shipInstance.TransformInverseRotation * shipInstance.WorldVelocity).z) > shipInstance.stuckSpeedThreshold)
                {
                    stuckTimer = 0f;
                    return false;
                }
                else
                {
                    stuckTimer += deltaTime;
                    return stuckTimer >= shipInstance.stuckTime;
                }
            }
        }

        /// <summary>
        /// Respawn (move) the ship to the closest point on the given Path.
        /// The ship must be initialised and the Path must be valid.
        /// </summary>
        /// <param name="pathguidHash"></param>
        /// <param name="updateVisibility"></param>
        /// <param name="resetVelocity"></param>
        /// <param name="isFixedUpdate"></param>
        private void RespawnOnPath(int pathguidHash, bool updateVisibility, bool resetVelocity, bool isFixedUpdate)
        {
            if (isInitialised && pathguidHash != 0)
            {
                PathData pathData = sscManager.GetPath(pathguidHash);
                if (pathData != null)
                {
                    Vector3 closestPointOnPath = Vector3.zero;
                    float closestPointOnPathTValue = 0f;
                    int prevPathLocationIdx = 0;

                    if (SSCMath.FindClosestPointOnPath(pathData, shipInstance.TransformPosition, ref closestPointOnPath, ref closestPointOnPathTValue, ref prevPathLocationIdx))
                    {
                        DisableShip(updateVisibility);

                        // Check for closest object
                        float lookDistance = 10f, minDistance = 10000f;
                        RaycastHit raycastHit;
                        Vector3 objectNormal = Vector3.zero;

                        Ray ray = new Ray(closestPointOnPath, Vector3.up);

                        SSCUtils.GetClosestCollider(ray, Vector3.up, lookDistance, ref minDistance, ref objectNormal, out raycastHit);
                        SSCUtils.GetClosestCollider(ray, Vector3.down, lookDistance, ref minDistance, ref objectNormal, out raycastHit);
                        SSCUtils.GetClosestCollider(ray, Vector3.left, lookDistance, ref minDistance, ref objectNormal, out raycastHit);
                        SSCUtils.GetClosestCollider(ray, Vector3.right, lookDistance, ref minDistance, ref objectNormal, out raycastHit);
                        SSCUtils.GetClosestCollider(ray, Vector3.forward, lookDistance, ref minDistance, ref objectNormal, out raycastHit);
                        SSCUtils.GetClosestCollider(ray, Vector3.back, lookDistance, ref minDistance, ref objectNormal, out raycastHit);

                        Vector3 respawnPos = Vector3.zero;
                        Quaternion respawnRot = Quaternion.identity;

                        if (objectNormal.sqrMagnitude > 0.01f)
                        {
                            Vector3 pathTangent = Vector3.zero;
                            // Get the direction of the path so ship faces in the correct direction AND is orientated upwards similar to closest object.
                            if (SSCMath.GetPathTangent(pathData, prevPathLocationIdx, closestPointOnPathTValue, ref pathTangent))
                            {
                                respawnPos = closestPointOnPath;
                                respawnRot = Quaternion.LookRotation(pathTangent, objectNormal);
                                //transform.SetPositionAndRotation(closestPointOnPath, Quaternion.LookRotation(pathTangent, objectNormal));
                            }
                            else
                            {
                                respawnPos = closestPointOnPath;
                                respawnRot = Quaternion.Euler(objectNormal);
                                //transform.SetPositionAndRotation(closestPointOnPath, Quaternion.Euler(objectNormal));
                            }
                        }
                        else
                        {
                            respawnPos = closestPointOnPath;
                            respawnRot = transform.rotation;
                            //transform.position = closestPointOnPath;
                        }

                        // If being run from FixedUpdate, move the rigidbody too
                        if (isFixedUpdate && rBody.isKinematic)
                        {
                            // Move the rigidbody (required for U2022+)
                            rBody.MovePosition(respawnPos);
                            rBody.MoveRotation(respawnRot);

                            // Set ship position and rotation
                            transform.SetPositionAndRotation(respawnPos, respawnRot);
                        }
                        else
                        {
                            // Set ship position and rotation
                            transform.SetPositionAndRotation(respawnPos, respawnRot);
                        }

                        EnableShip(updateVisibility, resetVelocity);
                    }
                }
            }
        }

        /// <summary>
        /// Check to see if there is a ShipAIInputModule attached to this ship.
        /// </summary>
        private void FetchShipAIInputModule()
        {
            isShipAIInputModuleAttached = TryGetComponent(out shipAIInputModule);
        }

        /// <summary>
        /// Check to see if there is a ShipDocking component attached to this ship
        /// </summary>
        private void FetchShipDocking()
        {
            isShipDockingAttached = TryGetComponent(out shipDocking);

            //shipDocking = GetComponent<ShipDocking>();
            //isShipDockingAttached = shipDocking != null;
        }

        /// <summary>
        /// [INTERNAL ONLY] Initialise radar for this ship
        /// Assumes shipInstance is not null and that
        /// sscRadar = SSCRadar.GetOrCreateRadar() has already
        /// been called.
        /// </summary>
        private void InitialiseRadar()
        {
            // Not assigned in the radar system
            shipInstance.radarItemIndex = -1;

            if (shipInstance.isRadarEnabled && sscRadar != null)
            {
                SSCRadarItem sscRadarItem = new SSCRadarItem();
                // No data from the ship yet, so hide it from radar
                sscRadarItem.isVisibleToRadar = false;
                sscRadarItem.blipSize = shipInstance.radarBlipSize;
                sscRadarItem.shipControlModule = this;

                // Create a packet to be used to send data to the radar system
                shipInstance.sscRadarPacket = new SSCRadarPacket();

                shipInstance.radarItemIndex = sscRadar.AddItem(sscRadarItem);
            }
        }

        /// <summary>
        /// [INTERNAL ONLY] Initialise radar for a localised damage region.
        /// Assumes shipInstance is not null, and that 
        /// sscRadar = SSCRadar.GetOrCreateRadar() has already been called.
        /// </summary>
        private void InitialiseLocalDamageRegionRadar(DamageRegion damageRegion)
        {
            // Not assigned in the radar system
            damageRegion.radarItemIndex = -1;

            if (shipInstance.isRadarEnabled && damageRegion.isRadarEnabled && sscRadar != null)
            {
                SSCRadarItem sscRadarItem = new SSCRadarItem();
                // No data from the ship damage region yet, so hide it from radar
                sscRadarItem.isVisibleToRadar = false;
                sscRadarItem.blipSize = 1;
                sscRadarItem.shipControlModule = this;
                // The damage region child transform can be used to help determine LoS. It may not be set.
                sscRadarItem.itemGameObject = damageRegion.regionChildTransform == null ? null : damageRegion.regionChildTransform.gameObject;
                sscRadarItem.radarItemType = SSCRadarItem.RadarItemType.ShipDamageRegion;
                sscRadarItem.guidHash = damageRegion.guidHash;

                // Create a packet to be used to send data to the radar system
                damageRegion.sscRadarPacket = new SSCRadarPacket();

                damageRegion.radarItemIndex = sscRadar.AddItem(sscRadarItem);
            }
        }

        /// <summary>
        /// If muzzle FX on weapons are pooled and have been parented to the weapons, when a
        /// ship or damage region is destroyed (o rmade inactive), they need to be reparented to the pool.
        /// NOTE: This may impact GC
        /// </summary>
        private void DestroyFX (Transform tfm)
        {
            EffectsModule[] effectsModules = tfm.GetComponentsInChildren<EffectsModule>(true);

            int numEffectsModules = effectsModules == null ? 0 : effectsModules.Length;

            for (int emIdx = 0; emIdx < numEffectsModules; emIdx++)
            {
                EffectsModule effectsModule = effectsModules[emIdx];

                if (effectsModule.usePooling && effectsModule.isReparented)
                {
                    effectsModule.DestroyEffectsObject();
                }
            }
        }

        /// <summary>
        /// Automatically attempt to determine what kind of ship this is for use in the radar system.
        /// Typically called after InitialiseRadar().
        /// It defaults ot PlayerShip.
        /// </summary>
        private void RadarAutoSetType()
        {
            if (sscRadar != null)
            {
                if (isShipAIInputModuleAttached) { sscRadar.SetItemType(shipInstance.RadarId, SSCRadarItem.RadarItemType.AIShip); }
                else { sscRadar.SetItemType(shipInstance.RadarId, SSCRadarItem.RadarItemType.PlayerShip); }
            }
        }

        /// <summary>
        /// Initiate a destruct prefab on a damage region of the ship
        /// NOTE: sscManager must not be null when calling this method.
        /// </summary>
        /// <param name="damageRegion"></param>
        /// <param name="isMainRegion"></param>
        private void DestructDamageRegion(DamageRegion damageRegion, bool isMainRegion)
        {
            if (!damageRegion.isDestructObjectActivated && damageRegion.destructObjectPrefabID >= 0 && damageRegion.destructObject != null)
            {
                Vector3 destructPosition = shipInstance.TransformPosition;
                Quaternion destructRotation = shipInstance.TransformRotation;

                if (isMainRegion)
                {
                    if (shipInstance.respawningMode == Ship.RespawningMode.DontRespawn)
                    {
                        // Turn off all colliders. As we are going to destroy the gameobject,
                        // we can simply deactivate it.
                        gameObject.SetActive(false);
                    }
                    else
                    {
                        // Cater for respawning - before DestructDamageRegion is called the ship is disabled
                        // with EnableOrDisableShip(false, true, false). Here we should not turn off the gameObject.
                        // This "seems" to work without having to disable the colliders but might need more testing.
                    }
                }
                else
                {
                    // Get the worldspace position of the region
                    destructPosition = shipInstance.GetDamageRegionWSPosition(damageRegion);

                    // Damage region needs to be repairable

                }

                // Instantiate the region destruct prefab
                InstantiateDestructParameters dstParms = new InstantiateDestructParameters
                {
                    destructPrefabID = damageRegion.destructObjectPrefabID,
                    position = destructPosition,
                    rotation = destructRotation,
                    explosionPowerFactor = 1f,
                    explosionRadiusFactor = 1f,
                    velocity = shipInstance.LocalVelocity
                };

                // Keep track of the DestructModule instance that was instantiated for this region
                if (sscManager.InstantiateDestruct(ref dstParms) != null)
                {
                    damageRegion.destructItemKey = new SSCDestructItemKey(damageRegion.destructObjectPrefabID, dstParms.destructPoolListIndex, dstParms.destructSequenceNumber);
                }
                damageRegion.isDestructObjectActivated = true;
            }
        }

        /// <summary>
        /// Update the Thruster Effects
        /// </summary>
        private void UpdateThrusterFX()
        {
            arrayLength = thrusterEffects == null ? 0 : thrusterEffects.Length;
            Vector3 localVelo = shipInstance.LocalVelocity;
            for (componentIndex = 0; componentIndex < arrayLength; componentIndex++)
            {
                if (thrusterEffects[componentIndex] != null)
                {
                    thrusterEffects[componentIndex].UpdateThrusterInput(shipInstance.thrusterList[componentIndex], localVelo);
                }
            }
        }

        #endregion

        #region Protected Virtual Methods

        /// <summary>
        /// Apply the resultant force and momement to the rigidbody.
        /// This is called from PhysicsSimulation(..) AFTER
        /// shipInstance.UpdatePositionAndMovementData(..) and
        /// CalculateForceAndMoment(..) have been called. 
        /// </summary>
        protected virtual void ApplyForceAndMoment()
        {
            // Apply the local resultant force and moment to the rigidbody

            // NOTE: AddRelativeForce is broken in 2023.2.0 to 2023.2.15 and <U6000.0.b13 as it does not apply the force at the centre of mass.
            // Instead, it appears to apply the force at the centre of the object (i.e., (0,0,0) in local space).
            // rBody.AddForceAtPosition(transform.rotation * localResultantForce, transform.TransformPoint(shipInstance.centreOfMass));

            rBody.AddRelativeForce(localResultantForce);
            rBody.AddRelativeTorque(localResultantMoment);
        }

        /// <summary>
        /// Override this if you want to use the OnDestroy() Unity event.
        /// To get notified if the ship is destroyed, instead subscribe to
        /// callbackOnDestroy. 
        /// </summary>
        protected virtual void OnDestroyGameObject()
        {

        }

        #endregion

        #region Events

        // Called when the ship rigidbody collides with another collider
        private void OnCollisionEnter(Collision collision)
        {
            if (callbackOnCollision != null) { callbackOnCollision.Invoke(this, collision); }
            else { shipInstance.ApplyCollisionDamage(collision); }
        }

        private void OnDestroy()
        {
            OnDestroyGameObject();
        }

        #endregion

        #region Public API Methods - Custom Timing and Physics

        /// <summary>
        /// This is typically called automatically during FixedUpdate().
        /// However, if PhysicsTiming is Manual this method should be called
        /// in your own code.
        /// </summary>
        /// <param name="physicsDeltaTime"></param>
        public void PhysicsSimulation (float physicsDeltaTime)
        {
            if (isInitialised)
            {
                if (!isRespawning)
                {
                    //float calcStartTime = Time.realtimeSinceStartup;

                    // When shipIsEnabled is false, isMovementEnabled is also false,
                    // though the opposite may not be true.
                    if (shipIsEnabled && isMovementEnabled)
                    {
                        #region Pass input / apply movement output

                        // Pass position and movement data to the ship
                        shipInstance.UpdatePositionAndMovementData(transform, rBody);
                        // Pass pilot movement input to the ship
                        shipInstance.pilotForceInput = pilotForceInput;
                        shipInstance.pilotMomentInput = pilotMomentInput;

                        // Calculate the local resultant force and moment
                        shipInstance.CalculateForceAndMoment(ref localResultantForce, ref localResultantMoment, physicsDeltaTime);

                        ApplyForceAndMoment();

                        // Has the ship run out of fuel?
                        if (!shipInstance.hasRemainingFuel && shipInstance.isThrusterSystemsStarted)
                        {
                            ShutdownThrusterSystems(true);
                        }

                        #endregion
                    }

                    //float calcEndTime = Time.realtimeSinceStartup;
                    //Debug.Log("| Total: " + ((calcEndTime - calcStartTime) * 1000f).ToString("0.0000") + " ms |");
                }
                else { CheckRespawnShip(true); }

                #region Timing Callback
                // Other components attached may need to run physics loop too
                // Avoid issue when gameobject is destroyed
                if (isInitialised && shipInstance.timingPhysicsLoopInt == Ship.SSCTimingManualInt)
                {
                    if (callbackOnPhysicsLoop != null) { callbackOnPhysicsLoop.Invoke(physicsDeltaTime); }
                }
                #endregion
            }
        }

        /// <summary>
        /// This is typically called automatically during Update().
        /// However, if UpdateTiming is Manual, this method should be called
        /// in your own code.
        /// </summary>
        /// <param name="updateDeltaTime"></param>
        public void UpdateLoop (float updateDeltaTime)
        {
            if (isInitialised)
            {
                if (isRespawning)
                {
                    #region Respawning

                    // If we are currently respawning, count down respawn timer
                    respawnTimer -= isRespawingPaused ? 0f : updateDeltaTime;

                    // See CheckRespawnShip(..) which is now called from FixedUpdate() to support U2022+

                    #endregion
                }
                else
                {
                    // Only apply input, calculate movement, update effects or process damage if the ship is enabled
                    if (shipIsEnabled)
                    {
                        // Weapons can only fire, respawning after a collision, when movement is enabled.
                        #region Pass input / apply combat output
                        if (isMovementEnabled || shipInstance.useWeaponsWhenMovementDisabled)
                        {
                            // Pass position and movement data to the ship
                            shipInstance.UpdatePositionAndMovementData(transform, rBody);
                            // Pass pilot weapons input to the ship
                            shipInstance.pilotPrimaryFireInput = pilotPrimaryFireInput;
                            shipInstance.pilotSecondaryFireInput = pilotSecondaryFireInput;
                            // Update weapons and damage data
                            shipInstance.UpdateWeaponsAndDamage(sscManager, updateDeltaTime);
                        }
                        #endregion

                        // Thrusters only work when movement is enabled
                        // OR when isThrusterFXStationary is true.
                        #region Visual/Audial Effects

                        // Behaviour change for 1.3.7 Beta 2a
                        if (shipInstance.isThrusterSystemsStarted && (isMovementEnabled || shipInstance.isThrusterFXStationary))
                        {
                            UpdateThrusterFX();
                        }
                        #endregion

                        #region Localised Damage Region Destruction, shield recharge, or Radar Update

                        if (shipInstance.shipDamageModel == Ship.ShipDamageModel.Localised &&
                            shipInstance.numLocalisedDamageRegions > 0 && sscManager != null)
                        {
                            // Loop through localised damage regions
                            DamageRegion damageRegion;
                            for (int d = 0; d < shipInstance.numLocalisedDamageRegions; d++)
                            {
                                damageRegion = shipInstance.localisedDamageRegionList[d];

                                #region Effects Object for damage region
                                // If a damage region has been "destroyed" but no effects object has been instantiated...
                                if (damageRegion.Health <= 0f && !damageRegion.isDestructionEffectsObjectInstantiated)
                                {
                                    // If there is an effects object for this damage region, instantiate it.
                                    if (damageRegion.effectsObjectPrefabID > 0)
                                    {
                                        InstantiateEffectsObjectParameters ieParms = new InstantiateEffectsObjectParameters
                                        {
                                            effectsObjectPrefabID = damageRegion.effectsObjectPrefabID,
                                            position = transform.TransformPoint(damageRegion.relativePosition),
                                            rotation = shipInstance.TransformRotation
                                        };

                                        // Keep track of the EffectsModule instance that was instantiated for this region
                                        if (sscManager.InstantiateEffectsObject(ref ieParms) != null)
                                        {
                                            damageRegion.destructionEffectItemKey = new SSCEffectItemKey(damageRegion.effectsObjectPrefabID, ieParms.effectsObjectPoolListIndex, ieParms.effectsObjectSequenceNumber);
                                        }
                                    }
                                    // Then remember that we have instantiated an effects object, so we don't do it again
                                    damageRegion.isDestructionEffectsObjectInstantiated = true;
                                }
                                // Check if the effects object should move with the ship
                                else if (damageRegion.isMoveDestructionEffectsObject && damageRegion.destructionEffectItemKey.effectsObjectSequenceNumber > 0)
                                {
                                    // Find the effects object, validate it is the correct one (it hasn't been despawned) and update position/rotation
                                    if (!sscManager.MoveEffectsObject(damageRegion.destructionEffectItemKey, transform.TransformPoint(damageRegion.relativePosition), shipInstance.TransformRotation, true))
                                    {
                                        // The effect may have been despawned
                                        // Once the effect has finished, it will not be respawned for this damage region
                                        damageRegion.destructionEffectItemKey = new SSCEffectItemKey(-1, -1, 0);
                                    }
                                }
                                #endregion

                                if (damageRegion.Health > 0f)
                                {
                                    #region Damage Region Radar Update
                                    if (shipInstance.isRadarEnabled && damageRegion.isRadarEnabled && damageRegion.radarItemIndex >= 0)
                                    {
                                        // Update fields that may change at any time
                                        damageRegion.sscRadarPacket.isVisibleToRadar = true;
                                        damageRegion.sscRadarPacket.position = shipInstance.GetDamageRegionWSPosition(damageRegion);
                                        damageRegion.sscRadarPacket.velocity = shipInstance.WorldVelocity;
                                        damageRegion.sscRadarPacket.velocity = shipInstance.WorldVelocity;
                                        damageRegion.sscRadarPacket.factionId = shipInstance.factionId;
                                        damageRegion.sscRadarPacket.squadronId = shipInstance.squadronId;

                                        // Use the internal radarItemIndex rather than RadarId to avoid the property lookup
                                        sscRadar.UpdateItem(damageRegion.radarItemIndex, damageRegion.sscRadarPacket);
                                    }
                                    #endregion

                                    #region Local Damage Region Shield Recharge
                                    damageRegion.CheckShieldRecharge();
                                    #endregion
                                }
                                // Only destroy the damage region once
                                else if (!damageRegion.isDestroyed)
                                {
                                    #region Damage Region Destruct
                                    DestructDamageRegion(damageRegion, false);

                                    if (damageRegion.regionChildTransform != null)
                                    {
                                        damageRegion.regionChildTransform.gameObject.SetActive(false);

                                        // Return any weapon muzzle FX to the pool
                                        DestroyFX(damageRegion.regionChildTransform);
                                    }

                                    // The damage region destroy action has occurred
                                    damageRegion.isDestroyed = true;

                                    if (damageRegion.isRadarEnabled) { DisableRadar(damageRegion); }

                                    #endregion
                                }
                            }
                        }

                        #endregion

                        #region Check if ship is stuck
                        if (isMovementEnabled && IsShipStuck(updateDeltaTime))
                        {
                            if (shipInstance.stuckAction == Ship.StuckAction.InvokeCallback)
                            {
                                if (callbackOnStuck != null) { callbackOnStuck(this); }
                            }
                            else if (shipInstance.stuckAction == Ship.StuckAction.RespawnOnPath)
                            {
                                // Stop camera shake if it is currently happening
                                if (shipInstance.callbackOnCameraShake != null) { shipInstance.callbackOnCameraShake(0f); }

                                RespawnOnPath(shipInstance.stuckActionPathGUIDHash, false, true, false);
                            }
                            else if (shipInstance.stuckAction == Ship.StuckAction.SameAsRespawningMode)
                            {
                                if (shipInstance.respawningMode != Ship.RespawningMode.DontRespawn)
                                {
                                    // We are now in the process of respawning
                                    isRespawning = true;
                                    // Set the respawn timer
                                    respawnTimer = shipInstance.respawnTime;

                                    // Stop camera shake if it is currently happening
                                    if (shipInstance.callbackOnCameraShake != null) { shipInstance.callbackOnCameraShake(0f); }

                                    // Now we want to "despawn" this ship
                                    // So we simply disable all visual and physics components

                                    // Disable the ship and make it invisible
                                    EnableOrDisableShip(false, true, false);
                                }
                            }
                        }
                        #endregion

                        #region Damage - Rumble and Camera Shake

                        if (isMovementEnabled && !shipInstance.Destroyed())
                        {
                            // Check if the last damage event index has changed
                            int thisDamageEventIndex = shipInstance.LastDamageEventIndex();
                            if (lastDamageEventIndex != thisDamageEventIndex)
                            {
                                // If the last damage event index has changed, there has been a damage event
                                lastDamageEventIndex = thisDamageEventIndex;

                                if (shipInstance.applyControllerRumble)
                                {
                                    // Apply rumble
                                    if (callbackOnRumble != null) { callbackOnRumble(shipInstance.RequiredDamageRumbleAmount()); }
                                    // Set the rumble timer
                                    damageRumbleTimer = 0.5f;
                                }

                                if (shipInstance.callbackOnCameraShake != null) { shipInstance.callbackOnCameraShake(shipInstance.RequiredCameraShakeAmount()); }
                            }
                            // NOTE: CameraShake is turned off by a timer in ShipCameraModule or by calling StopCameraShake(..)
                            else if (shipInstance.applyControllerRumble)
                            {
                                // Count down the rumble timer
                                if (damageRumbleTimer > 0f)
                                {
                                    damageRumbleTimer -= updateDeltaTime;
                                }
                                // If the timer reaches zero, disable rumble
                                if (damageRumbleTimer < 0f)
                                {
                                    if (callbackOnRumble != null) { callbackOnRumble(0f); }
                                }
                            }
                        }

                        #endregion

                        #region Docking
                        // If ship is still enabled, perform dock action
                        if (pilotDockInput && shipIsEnabled && isShipDockingAttached && shipDocking.IsInitialised)
                        {
                            // Currently only supports Docked and Undocked for player ships
                            if (shipDocking.GetStateInt() == ShipDocking.dockedInt)
                            {
                                shipDocking.SetState(ShipDocking.DockingState.NotDocked);
                            }
                            else
                            {
                                shipDocking.SetState(ShipDocking.DockingState.Docked);
                            }
                        }
                        #endregion
                    }

                    #region Ship Destruction OR Ship radar update and Thruster Systems startup/shutdown

                    // Check if the ship has been destroyed
                    if (shipInstance.Destroyed())
                    {
                        // Stop camera shake if it is currently happening
                        if (shipInstance.callbackOnCameraShake != null) { shipInstance.callbackOnCameraShake(0f); }

                        DestroyFX(transform);

                        if (callbackOnDestroy != null) { callbackOnDestroy(this.shipInstance); }

                        // Check if the ship should be respawned
                        if (shipInstance.respawningMode != Ship.RespawningMode.DontRespawn)
                        {
                            // We are now in the process of respawning
                            isRespawning = true;
                            // Set the respawn timer
                            respawnTimer = shipInstance.respawnTime;

                            // Now we want to "despawn" this ship
                            // So we simply disable all visual and physics components

                            // Disable the ship and make it invisible
                            EnableOrDisableShip(false, true, false);

                            if (sscManager != null)
                            {
                                #region Instantiate the ship destruction effects prefab
                                if (shipInstance.mainDamageRegion.destructionEffectsObject != null)
                                {
                                    InstantiateEffectsObjectParameters ieParms = new InstantiateEffectsObjectParameters
                                    {
                                        effectsObjectPrefabID = shipInstance.mainDamageRegion.effectsObjectPrefabID,
                                        position = transform.position,
                                        rotation = transform.rotation
                                    };

                                    sscManager.InstantiateEffectsObject(ref ieParms);
                                }
                                #endregion

                                DestructDamageRegion(shipInstance.mainDamageRegion, true);
                            }

                            // End the rumble timer
                            damageRumbleTimer = -1f;
                        }
                        else
                        {
                            shipInstance.DeactivateBeams(sscManager);

                            // If the ship is using radar, remove it from radar before the ship gameobject
                            // is destroyed. Also remove any Localised Damage Regions from radar.
                            if (shipInstance.isRadarEnabled && shipInstance.radarItemIndex >= 0)
                            {
                                DisableRadar();
                                //sscRadar.DisableRadar(shipInstance.radarItemIndex);
                            }

                            if (sscManager != null)
                            {
                                #region Instantiate the ship destruction effects prefab
                                if (shipInstance.mainDamageRegion.destructionEffectsObject != null)
                                {
                                    InstantiateEffectsObjectParameters ieParms = new InstantiateEffectsObjectParameters
                                    {
                                        effectsObjectPrefabID = shipInstance.mainDamageRegion.effectsObjectPrefabID,
                                        position = transform.position,
                                        rotation = transform.rotation
                                    };

                                    sscManager.InstantiateEffectsObject(ref ieParms);
                                }
                                #endregion

                                DestructDamageRegion(shipInstance.mainDamageRegion, true);
                            }

                            // Destroy this ship
                            // Prevent the shipInstance referencing the ShipControlModule transform.
                            if (isInitialised) { shipInstance.shipTransform = null; }
                            Destroy(gameObject);
                            // Need to also destroy the ship class instance
                            if (shipInstance != null) { isInitialised = false; shipInstance = null; }
                        }
                    }
                    else if (shipIsEnabled)
                    {
                        if (shipInstance.isRadarEnabled && !isTransitioning)
                        {
                            // Update fields that may change at any time
                            shipInstance.sscRadarPacket.isVisibleToRadar = true;
                            shipInstance.sscRadarPacket.position = shipInstance.TransformPosition;
                            shipInstance.sscRadarPacket.velocity = shipInstance.WorldVelocity;
                            shipInstance.sscRadarPacket.factionId = shipInstance.factionId;
                            shipInstance.sscRadarPacket.squadronId = shipInstance.squadronId;

                            // Use the internal radarItemIndex rather than RadarId to avoid the property lookup
                            sscRadar.UpdateItem(shipInstance.radarItemIndex, shipInstance.sscRadarPacket);
                        }

                        bool hasStarted, hasShutdown;
                        if (shipInstance.CheckThrusterSystems(updateDeltaTime, out hasStarted, out hasShutdown))
                        {
                            if (hasShutdown)
                            {
                                //Debug.Log("[DEBUG] StopThrusterEffects " + name + " T:" + Time.time);
                                StopThrusterEffects();
                            }

                            // CheckThrusterSystems should only return Started or Shutdown
                            if (callbackOnThrusterSystemsChanged != null) { callbackOnThrusterSystemsChanged.Invoke(this, hasShutdown ? 3 : 1); }
                        }

                        shipInstance.mainDamageRegion.CheckShieldRecharge();
                    }

                    #endregion
                }

                #region Timing Callback
                // Other components attached may need to run update loop too
                // Avoid issue with ship gameobject being destroyed above
                if (isInitialised && shipInstance.timingUpdateLoopInt == Ship.SSCTimingManualInt)
                {
                    if (callbackOnUpdateLoop != null) { callbackOnUpdateLoop.Invoke(updateDeltaTime); }
                }
                #endregion
            }
        }

        #endregion

        #region Public API Methods - Initialisation

        /// <summary>
        /// Check if the ship has moved to another scene and if required,
        /// update the reference to the SSCManager in that new scene.
        /// </summary>
        /// <returns></returns>
        public bool CheckSceneChanged ()
        {
            bool hasSceneChanged = false;

            int oldScenehandle = sceneHandle;

            sceneHandle = gameObject.scene.handle;

            hasSceneChanged = oldScenehandle != sceneHandle;

            if (!isInitialised || hasSceneChanged)
            {
                sscManager = SSCManager.GetOrCreateManager(sceneHandle);
            }

            return hasSceneChanged;
        }

        /// <summary>
        /// Attempt to clear the SSCManager. This is required when moving the ship
        /// to a different scene using SSC Seamless (X Pack 2).
        /// Notify subscribers to callbackOnClearManager.
        /// See also RefreshManager().
        /// </summary>
        public void ClearManager()
        {
            if (isInitialised)
            {
                #region Clear weapon data.
                // Not sure what will happen if a beam weapon is firing when this occurs...
                int _numWeapons = NumberOfWeapons;
                for (int wIdx = 0; wIdx < _numWeapons; wIdx++)
                {
                    Weapon weapon = shipInstance.weaponList[wIdx];
                    if (weapon != null)
                    {
                        weapon.ClearTarget();
                        weapon.projectilePrefabID = -1;
                        weapon.beamPrefabID = -1;
                    }
                }
                #endregion

                #region Damage Region data

                DamageRegion damageRegion = shipInstance.mainDamageRegion;
                if (damageRegion != null)
                {
                    damageRegion.destructObjectPrefabID = -1;
                    damageRegion.effectsObjectPrefabID = -1;
                }

                int numLDamageRegions = shipInstance.localisedDamageRegionList == null ? 0 : shipInstance.localisedDamageRegionList.Count;
                
                for (int drIdx = 0; drIdx < numLDamageRegions; drIdx++)
                {
                    damageRegion = shipInstance.localisedDamageRegionList[drIdx];
                    if (damageRegion != null)
                    {
                        damageRegion.destructObjectPrefabID = -1;
                        damageRegion.effectsObjectPrefabID = -1;
                    }
                }

                #endregion

                #region Radar

                sscRadar = null;

                #endregion

                // Notify any subscribers
                if (callbackOnClearManager != null) { callbackOnClearManager.Invoke(); }

                sscManager = null;
            }
        }

        /// <summary>
        /// Runs all necessary initialisation processes for the ship.
        /// </summary>
        public virtual void InitialiseShip()
        {
            if (!isInitialised)
            {
                // Find the rigidbody
                rBody = GetComponent<Rigidbody>();

                // Cater for scenario where [RequireComponent(typeof(Rigidbody))] doesn't work
                // RequireComponent only seems to work when first adding the script to a gameobject.
                if (rBody == null) { isInitialised = false; return; }
                // Configure the rigidbody
                #if UNITY_6000_0_OR_NEWER
                rBody.linearDamping = 0f;
                rBody.angularDamping = 0f;
                #else
                rBody.drag = 0f;
                rBody.angularDrag = 0f;
                #endif
                rBody.useGravity = false;
                rBody.isKinematic = false;
                rBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                // Set the mass and centre of mass of the ship
                ReinitialiseMass();

                sceneHandle = gameObject.scene.handle;

                // Set timing before updating position data
                shipInstance.SetTimingPhysicsLoop(shipInstance.timingPhysicsLoop);
                shipInstance.SetTimingUpdateLoop(shipInstance.timingUpdateLoop);
                shipInstance.SetMoveDataUpdate(shipInstance.moveDataUpdate);

                // Initialise all necessary ship data
                shipInstance.UpdatePositionAndMovementData(transform, rBody);
                shipInstance.Initialise(transform);

                // Initialise ship health
                shipInstance.ResetHealth();

                ReinitialiseThrusterEffects();

                if (!shipInstance.isThrusterSystemsStarted)
                {
                    StopThrusterEffects();
                }

                // Get a reference to the Ship Controller Manager instance
                CheckSceneChanged();

                RefreshManager();

                // Initialise required lists
                // Pre-size the list of renderers that is used when
                // ships are disable/enabled during respawing. If the list
                // is too small, it be expanded the first time it is used (and affect GC).
                activeRenderersList = new List<Renderer>(5);

                // Assume only 1 audio source per ship. Will auto expand if required.
                // Increase the capacity of the list if ships tend to have more audio sources.
                audioSourcesList = new List<AudioSource>(1);

                FetchShipAIInputModule();
                FetchShipDocking();

                ReinitialiseRadar();

                // Keep compiler happy
                if (isTransitioning) { }

                isInitialised = true;
            }
        }

        /// <summary>
        /// Initialise or reinitialise objects managed by SSCManager including:
        /// 1. projectiles and effects objects
        /// 2. beam weapons and the effects objects used by those weapons
        /// 3. destruct objects used by damage regions
        /// 4. Notify any subsribers to callbackOnRefreshManager
        /// USAGE: If (CheckSceneChanged() { RefreshManager(); }
        /// </summary>
        public void RefreshManager()
        {
            // Reinitialise projectiles and effects objects
            ReinitialiseShipProjectilesAndEffects();

            // Reinitialise beam weapons and the effects objects used by those weapons.
            ReinitialiseShipBeams();

            // Reinitialise destruct objects used by damage regions
            ReinitialiseShipDestructObjects();

            // Notify any subscribers
            if (callbackOnRefreshManager != null) { callbackOnRefreshManager.Invoke(sscManager); }
        }

        /// <summary>
        /// Typically called automatically but you may need to call this if you move the ship to a different scene.
        /// </summary>
        public void ReinitialiseRadar()
        {
            if (shipInstance.isRadarEnabled)
            {
                sscRadar = SSCRadar.GetOrCreateRadar(sceneHandle);
                InitialiseRadar();
                RadarAutoSetType();

                if (shipInstance.shipDamageModel == Ship.ShipDamageModel.Localised)
                {
                    for (int dmIdx = 0; dmIdx < shipInstance.numLocalisedDamageRegions; dmIdx++)
                    {
                        InitialiseLocalDamageRegionRadar(shipInstance.localisedDamageRegionList[dmIdx]);
                    }
                }
            }
        }

        /// <summary>
        /// Reinitialises variables related to the mass of the ship.
        /// Call after modifying shipInstance.mass or shipInstance.centreOfMass.
        /// </summary>
        public void ReinitialiseMass()
        {
            // Set the mass and centre of mass of the ship
            #if UNITY_2022_2_OR_NEWER
            rBody.automaticCenterOfMass = !shipInstance.setCentreOfMassManually;
            #endif
            rBody.mass = shipInstance.mass;
            rBody.centerOfMass = shipInstance.centreOfMass;
        }

        /// <summary>
        /// Reinitialises variables required for projectiles and effects of the ship.
        /// Call this after modifying any projectile (or missiles from SSC Expansion Pack 1)
        /// or effect data for this ship.
        /// Also call after adding one or more projectile weapons with shipInstance.AddWeapon(..).
        /// </summary>
        public void ReinitialiseShipProjectilesAndEffects()
        {
            if (sscManager != null)
            {
                // Initialise projectiles and effects objects
                sscManager.UpdateProjectilesAndEffects(shipInstance);
            }
            #if UNITY_EDITOR
            else
            {
                Debug.LogWarning("ShipControlModule.ReinitialiseShipProjectilesAndEffects Warning: could not find SSCManager to update projectiles and effects.");
            }
            #endif
        }

        /// <summary>
        /// Reinitialises variables required for destruct objects of the ship.
        /// Call after modifying any destruct data for this ship.
        /// </summary>
        public void ReinitialiseShipDestructObjects ()
        {
            if (sscManager != null)
            {
                // Initialise destruct objects
                sscManager.UpdateDestructObjects(shipInstance);
            }
            #if UNITY_EDITOR
            else
            {
                Debug.LogWarning("ShipControlModule.ReinitialiseShipDestructObjects Warning: could not find SSCManager to update destruct objects.");
            }
            #endif
        }

        /// <summary>
        /// Reinitialises variables required for ship beam weapons and effects used by those beams.
        /// Call this after modifying any beams or beam effect data for this ship.
        /// Call this after adding one or more Beam weapons using shipInstance.AddWeapon(..).
        /// </summary>
        public void ReinitialiseShipBeams()
        {
            if (sscManager != null)
            {
                // Initialise beams, and effects objects used by those beams
                sscManager.UpdateBeamsAndEffects(shipInstance);
            }
            #if UNITY_EDITOR
            else
            {
                Debug.LogWarning("ShipControlModule.ReinitialiseShipBeams Warning: could not find SSCManager to update (weapon) beams");
            }
            #endif
        }

        /// <summary>
        /// Reinitialises the thruster effects for a ship.
        /// Call this after modifying thruster effects objects for this ship.
        /// WARNING: This will generate Garbage so use sparingly.
        /// </summary>
        public void ReinitialiseThrusterEffects()
        {
            // Initialise all thruster effects
            arrayLength = shipInstance.thrusterList == null ? 0 : shipInstance.thrusterList.Count;
            int numThrusterEffects = thrusterEffects == null ? 0 : thrusterEffects.Length;
            if (arrayLength > 0)
            {
                // There is a list of thrusterEffectObjects. Each thruster has a slot however it also may be null if the thruster has no effects parent gameobject.
                // Each thruster should have a single ThrusterEffects slot (which could be null if thruster has no effects parent gameobject in the scene)
                // One ThrusterEffects component is added the to thruster effects parent gameobject (if there is one) for each Thruster in the scene.
                // Each ThrusterEffects component is then initialised.

                if (numThrusterEffects == 0) { thrusterEffects = new ThrusterEffects[arrayLength]; }
                else if (arrayLength != numThrusterEffects) { System.Array.Resize(ref thrusterEffects, arrayLength); }

                numThrusterEffects = thrusterEffects == null ? 0 : thrusterEffects.Length;

                for (componentIndex = 0; componentIndex < arrayLength; componentIndex++)
                {
                    if (thrusterEffectObjects[componentIndex] != null)
                    {
                        // Check if the ThrusterEffects component has previously been added. If not, add it now.
                        ThrusterEffects thrusterEffect = thrusterEffectObjects[componentIndex].GetComponent<ThrusterEffects>();
                        if (thrusterEffect == null) { thrusterEffect = thrusterEffectObjects[componentIndex].AddComponent<ThrusterEffects>(); }

                        thrusterEffects[componentIndex] = thrusterEffect;
                        thrusterEffects[componentIndex].Initialise();
                    }
                    else
                    {

                        //thrusterEffects[componentIndex].Clear();
                    }
                }
            }
            else
            {
                // Clean up old thruster effect class instances
                if (numThrusterEffects > 0)
                {
                    for (componentIndex = 0; componentIndex < numThrusterEffects; componentIndex++)
                    {
                        thrusterEffects[componentIndex].Clear();
                    }
                    System.Array.Clear(thrusterEffects, 0, numThrusterEffects);
                }
                thrusterEffects = null;
            }
        }

        #endregion

        #region  Public API Methods - Reset, Enable, Disable Ship
       
        /// <summary>
        /// A fast way of re-initialising a ship without changing its position or core
        /// settings. Typically called when re-initialising a scene or bringing a ship
        /// out of hibernation. If you want to temporarily stop a ship from moving, like
        /// when a user brings up a menu, call DisableShip() and EnableShip() instead.
        /// </summary>
        public void ResetShip()
        {
            // Temporily prevent other things from occuring
            isInitialised = false;

            // Disable ship but don't make it invisible. Reset velocity
            EnableOrDisableShip(false, false, true);

            // Stop all Thrusters on the ship
            arrayLength = thrusterEffects == null ? 0 : thrusterEffects.Length;
            for (componentIndex = 0; componentIndex < arrayLength; componentIndex++)
            {
                if (thrusterEffects[componentIndex] != null)
                {
                    thruster = shipInstance.thrusterList[componentIndex];
                    if (thruster != null) { thrusterEffects[componentIndex].Stop(); }
                }
            }

            isRespawning = false;

            // (Re)initialise all necessary ship data
            shipInstance.UpdatePositionAndMovementData(transform, rBody);

            // NOTE: This may cause issues with Radar as it sets radarItemIndex = -1
            // If we see a problem, maybe we need to remove from radar first.
            shipInstance.Initialise(transform);

            // Reset the health of the ship
            shipInstance.ResetHealth();

            // Get the last damage event index of the ship
            lastDamageEventIndex = shipInstance.LastDamageEventIndex();

            // Reset rumble
            damageRumbleTimer = -1f;

            isInitialised = true;
        }

        /// <summary>
        /// Enables the ship and make visible.
        /// If resetVelocity is set to true, resets the velocity of the ship to zero.
        /// </summary>
        /// <param name="resetVelocity"></param>
        public void EnableShip (bool resetVelocity)
        {
            EnableOrDisableShip(true, true, resetVelocity);
        }

        /// <summary>
        /// Enables the ship. If updateVisibility is set to true, makes the ship visible.
        /// If resetVelocity is set to true, resets the velocity of the ship to zero.
        /// </summary>
        /// <param name="updateVisibility"></param>
        /// <param name="resetVelocity"></param>
        public void EnableShip(bool updateVisibility, bool resetVelocity)
        {
            EnableOrDisableShip(true, updateVisibility, resetVelocity);
        }

        /// <summary>
        /// Disables the ship. If updateVisibility is set to true, makes the ship invisible.
        /// </summary>
        /// <param name="updateVisibility"></param>
        public void DisableShip(bool updateVisibility)
        {
            EnableOrDisableShip(false, updateVisibility, false);
        }

        /// <summary>
        /// Returns whether the ship is currently enabled. When disabled, it
        /// cannot move, be visible or radar, nor received damage and be destroyed.
        /// </summary>
        /// <returns></returns>
        public bool ShipIsEnabled()
        {
            return shipIsEnabled;
        }
        #endregion

        #region Public API Methods - Enable, Disable Ship Movement

        /// <summary>
        /// Returns whether the ship movement is curently enabled.
        /// See also ShipIsEnabled(). A ship's movement may be disabled
        /// while it can still receive damaage, be destroyed or be visible
        /// to radar.
        /// </summary>
        /// <returns></returns>
        public bool ShipMovementIsEnabled()
        {
            return isMovementEnabled;
        }

        /// <summary>
        /// This is a subset of EnableShip(...).
        /// It only applies to:
        /// 1) Physics
        /// 2) Thruster Effects
        /// 3) User or AI Input to the Ship
        /// 4) Sound FX (audio)
        /// If a ship is also disabled, this will re-enable the ship.
        /// </summary>
        /// <param name="resetVelocity"></param>
        public void EnableShipMovement(bool resetVelocity)
        {
            if (isInitialised)
            {
                if (!shipIsEnabled) { EnableOrDisableShip(true, true, resetVelocity); }
                else { EnableOrDisableShipMovement(true, resetVelocity); }
            }
        }

        /// <summary>
        /// This is a subset of DisableShip(...).
        /// It only applies to:
        /// 1) Physics
        /// 2) Thruster Effects
        /// 3) User or AI Input to the Ship
        /// 4) Sound FX (audio)
        /// </summary>
        public void DisableShipMovement()
        {
            if (isInitialised)
            {
                EnableOrDisableShipMovement(false, false);
            }
        }

        /// <summary>
        /// Teleport the ship to a new location by moving by an amount
        /// in the x, y and z directions. This could be useful if changing
        /// the origin or centre of your world to compensate for float-point
        /// error.
        /// NOTE: This does not alter the current Respawn position.
        /// </summary>
        /// <param name="delta"></param>
        /// <param name="resetVelocity"></param>
        public void TelePort (Vector3 delta, bool resetVelocity)
        {
            // Do we need to re-enable movement?
            bool isMovementEnabled = false;

            Vector3 _origVelo = isInitialised ? shipInstance.WorldVelocity : Vector3.zero;
            Vector3 _origAVelo = isInitialised ? shipInstance.WorldAngularVelocity : Vector3.zero;

            if (isInitialised) { DisableShipMovement(); isMovementEnabled = true; }

            transform.position += delta;

            // If movement was enabled, re-enable it
            if (isMovementEnabled) { EnableShipMovement(resetVelocity); }

            if (isInitialised)
            {
                if (!resetVelocity)
                {
                    #if UNITY_6000_0_OR_NEWER
                    rBody.linearVelocity = _origVelo;
                    #else
                    rBody.velocity = _origVelo;
                    #endif
                    rBody.angularVelocity = _origAVelo;
                }

                shipInstance.UpdatePositionAndMovementData(transform, rBody);
            }
        }

        /// <summary>
        /// Teleport the ship to a new location with a new rotation.
        /// NOTE: This does not alter the current Respawn position.
        /// </summary>
        /// <param name="newPosition"></param>
        /// <param name="newRotation"></param>
        /// <param name="resetVelocity"></param>
        /// <param name="delayColliderUpdate">Force all attached colliders to update their position relative to the rigidbody in this frame</param>
        public void TelePort (Vector3 newPosition, Quaternion newRotation, bool resetVelocity, bool delayColliderUpdate = false)
        {
            // Do we need to re-enable movement?
            bool isMovementEnabled = false;

            Vector3 _origVelo = isInitialised ? shipInstance.WorldVelocity : Vector3.zero;
            Vector3 _origAVelo = isInitialised ? shipInstance.WorldAngularVelocity : Vector3.zero;

            if (isInitialised) { DisableShipMovement(); isMovementEnabled = true; }

            // Changed in v1.4.0 to set the rbody rather than force all attached colliders to recalculate their positions relative to the rbody.
            if (delayColliderUpdate && isInitialised)
            {
                rBody.position = newPosition;
                rBody.rotation = newRotation;
            }
            else
            {
                transform.SetPositionAndRotation(newPosition, newRotation);
            }

            //if (resetVelocity && isInitialised) { shipInstance.ResetVelocityData(); }

            // If movement was enabled, re-enable it
            if (isMovementEnabled) { EnableShipMovement(resetVelocity); }

            if (isInitialised)
            {
                if (!resetVelocity)
                {
                    #if UNITY_6000_0_OR_NEWER
                    rBody.linearVelocity = _origVelo;
                    #else
                    rBody.velocity = _origVelo;
                    #endif
                    rBody.angularVelocity = _origAVelo;
                }

                shipInstance.UpdatePositionAndMovementData(transform, rBody);
            }
        }

        #endregion

        #region Public API Methods - Respawning

        /// <summary>
        /// If the ship is currently respawning, this will stop
        /// the countdown timer, preventing the ship from respawning
        /// until ResumeRespawning() is called.
        /// NOTE: Has no effect if not already respawning.
        /// </summary>
        public void PauseRespawning()
        {
            if (isRespawning) { isRespawingPaused = true; }
        }

        /// <summary>
        /// If respawning is currently paused, the respawning
        /// timer will now continue until the ship is respawned.
        /// NOTE: Has no effect if respawningMode is DontRespawn
        /// </summary>
        public void ResumeRespawning()
        {
            isRespawingPaused = false;
        }

        #endregion

        #region  Public API Methods - Radar

        /// <summary>
        /// The ship will no longer send tracking information to the radar system.
        /// If you want to change the visibility to other radar consumers, consider
        /// changing the radar item data rather than disabling the radar and (later)
        /// calling EnableRadar again. When using a Localised ShipDamageModel, it
        /// will also disable radar on all localised Damage Regions.
        /// </summary>
        public void DisableRadar()
        {
            if (isInitialised)
            {
                if (shipInstance.isRadarEnabled && sscRadar != null && sscRadar.IsInitialised)
                {
                    // If enabled, remove the localised damage regions from radar
                    if (shipInstance.shipDamageModel == Ship.ShipDamageModel.Localised)
                    {
                        for (int dmIdx = 0; dmIdx < shipInstance.numLocalisedDamageRegions; dmIdx++)
                        {
                            DisableRadar(shipInstance.localisedDamageRegionList[dmIdx]);  
                        }
                    }

                    // Remove this ship from radar
                    sscRadar.RemoveItem(shipInstance.radarItemIndex);
                }
                shipInstance.sscRadarPacket = null;
                shipInstance.isRadarEnabled = false;
                shipInstance.radarItemIndex = -1;
            }
            sscRadar = null;
        }

        /// <summary>
        /// The ship will no longer send tracking information to the radar system for this
        /// damage region. If you want to change the visibility to other radar consumers, consider
        /// changing the radar item data rather than disabling the radar and (later)
        /// calling EnableRadar(damageRegion) again.
        /// NOTE: You do not need to call this if calling DisableRadar() for the ship.
        /// </summary>
        /// <param name="damageRegion"></param>
        public void DisableRadar(DamageRegion damageRegion)
        {
            if (damageRegion != null)
            {
                if (damageRegion.isRadarEnabled && damageRegion.radarItemIndex >= 0)
                {
                    sscRadar.RemoveItem(damageRegion.radarItemIndex);
                }
                damageRegion.sscRadarPacket = null;
                damageRegion.isRadarEnabled = false;
                damageRegion.radarItemIndex = -1;
            }
        }

        /// <summary>
        /// Enable the ship to send tracking information to the
        /// radar system. The ship must first be initialised.
        /// </summary>
        public void EnableRadar()
        {
            if (isInitialised)
            {
                sscRadar = SSCRadar.GetOrCreateRadar(sceneHandle);

                if (sscRadar != null)
                {
                    shipInstance.isRadarEnabled = true;
                    InitialiseRadar();
                    RadarAutoSetType();
                }
            }
        }

        /// <summary>
        /// Enable the ship to send tracking information to the radar system
        /// for this damage region. The ship must be initialised and radar must
        /// already be enabled for the ship. See also EnableRadar().
        /// </summary>
        /// <param name="damageRegion"></param>
        public void EnableRadar(DamageRegion damageRegion)
        {
            if (isInitialised && damageRegion.radarItemIndex == -1)
            {
                InitialiseLocalDamageRegionRadar(damageRegion);
                damageRegion.isRadarEnabled = damageRegion.radarItemIndex >= 0;
            }
        }

        #endregion

        #region  Public API Methods - Ship Input

        /// <summary>
        /// Provide an instance of shipInput, and have it populated with the current values from the ship.
        /// </summary>
        /// <param name="shipInput"></param>
        public void GetShipInput (ShipInput shipInput)
        {
            if (shipInput != null)
            {
                shipInput.horizontal = pilotForceInput.x;
                shipInput.vertical = pilotForceInput.y;
                shipInput.longitudinal = pilotForceInput.z;
                shipInput.pitch = pilotMomentInput.x;
                shipInput.yaw = pilotMomentInput.y;
                shipInput.roll = pilotMomentInput.z;
                shipInput.primaryFire = pilotPrimaryFireInput;
                shipInput.secondaryFire = pilotSecondaryFireInput;
                shipInput.dock = pilotDockInput;
            }
        }

        /// <summary>
        /// Sends the specified input to the ship control module in order to control the ship.
        /// By default all data inputs should be enabled even when sending 0 values.
        /// </summary>
        /// <param name="shipInput"></param>
        public void SendInput(ShipInput shipInput)
        {
            if (shipInput.isHorizontalDataEnabled) { pilotForceInput.x = shipInput.horizontal; }
            if (shipInput.isVerticalDataEnabled) { pilotForceInput.y = shipInput.vertical; }
            if (shipInput.isLongitudinalDataEnabled) { pilotForceInput.z = shipInput.longitudinal; }

            if (shipInput.isPitchDataEnabled) { pilotMomentInput.x = shipInput.pitch; }
            if (shipInput.isYawDataEnabled) { pilotMomentInput.y = shipInput.yaw; }
            if (shipInput.isRollDataEnabled) { pilotMomentInput.z = shipInput.roll; }

            if (shipInput.isPrimaryFireDataEnabled) { pilotPrimaryFireInput = shipInput.primaryFire; }
            if (shipInput.isSecondaryFireDataEnabled) { pilotSecondaryFireInput = shipInput.secondaryFire; }
            if (shipInput.isDockDataEnabled) { pilotDockInput = shipInput.dock; }
        }
        #endregion

        #region Public API Methods - Docking

        /// <summary>
        /// Is the ShipDocking component attached to this ship, and if so, is the ship's state 'Docked'?
        /// NOTE: This does not mean it must be docked with a ShipDockingStation. For that, you would
        /// need to check the ShipDockingStation's docking points.
        /// </summary>
        /// <returns></returns>
        public bool ShipIsDocked()
        {
            return isShipDockingAttached && shipDocking.GetState() == ShipDocking.DockingState.Docked;
        }

        /// <summary>
        /// Is the ShipDocking component attached to this ship, and if so, is the ship's state 'Not Docked'?
        /// NOTE: This does not mean it must be not docked with a ShipDockingStation. For that, you would
        /// need to check the ShipDockingStation's docking points.
        /// </summary>
        /// <returns></returns>
        public bool ShipIsNotDocked()
        {
            return isShipDockingAttached && shipDocking.GetStateInt() == ShipDocking.notDockedInt;
        }

        /// <summary>
        /// Retrieves a reference to the ShipDocking script if one was attached at the time this
        /// module was initiated.
        /// </summary>
        /// <param name="forceCheck">Ignore cached value and call GetComponent when true</param>
        /// <returns></returns>
        public ShipDocking GetShipDocking(bool forceCheck = false)
        {
            if (forceCheck) { FetchShipDocking(); }
            return isShipDockingAttached ? shipDocking : null;
        }

        #endregion

        #region Public API Methods - Paths

        /// <summary>
        /// Attempt to move the ship to the closest point on a path
        /// </summary>
        /// <param name="pathguidHash"></param>
        /// <param name="updateVisibility"></param>
        /// <param name="resetVelocity"></param>
        /// <param name="isFixedUpdate"></param>
        public void MoveToPath (int pathguidHash, bool updateVisibility, bool resetVelocity, bool isFixedUpdate)
        {
            RespawnOnPath(pathguidHash, updateVisibility, resetVelocity, isFixedUpdate);
        }

        /// <summary>
        /// Attempt to move the ship to the closest point on a path
        /// </summary>
        /// <param name="path"></param>
        /// <param name="updateVisibility"></param>
        /// <param name="resetVelocity"></param>
        /// <param name="isFixedUpdate"></param>
        public void MoveToPath (PathData path, bool updateVisibility, bool resetVelocity, bool isFixedUpdate)
        {
            if (path != null)
            {
                RespawnOnPath (path.guidHash, updateVisibility, resetVelocity, isFixedUpdate);
            }
        }

        #endregion

        #region Public API Methods - Ship AI

        /// <summary>
        /// Retrieves a reference to the ShipAIInputModule script if one was attached at the time
        /// this module was initialised.
        /// </summary>
        /// <param name="forceCheck">Ignore cached value and call TryGetComponent when true</param>
        /// <returns></returns>
        public ShipAIInputModule GetShipAIInputModule (bool forceCheck = false)
        {
            if (forceCheck) { FetchShipAIInputModule(); }
            return isShipAIInputModuleAttached ? shipAIInputModule : null;
        }

        /// <summary>
        /// Retrieves a reference to the ShipAIInputModule script if one was attached at the time
        /// this module was initialised.
        /// </summary>
        /// <param name="shipAIInputModule"></param>
        /// <param name="forceCheck">Ignore cached value and call TryGetComponent when true</param>
        /// <returns>True if a shipAIInputModule was detected or cached</returns>
        public bool GetShipAIInputModule (out ShipAIInputModule shipAIInputModule, bool forceCheck = false)
        {
            if (forceCheck) { FetchShipAIInputModule(); }

            shipAIInputModule = this.shipAIInputModule;

            return isShipAIInputModuleAttached;
        }

        #endregion

        #region Public API Methods - Thrusters

        /// <summary>
        /// Add 1 second of forward boost.
        /// See also StopBoost().
        /// For more control, see shipInstance.AddBoost(..).
        /// </summary>
        /// <param name="forceAmountKNeutons"></param>
        public void AddBoost (float forceAmountKNeutons)
        {
            if (isInitialised)
            {
                shipInstance.AddBoost(Vector3.forward, forceAmountKNeutons * 1000f, 1f);
            }
        }

        /// <summary>
        /// Enable all the thruster effects where the gameobject contains the specified string.
        /// Typically used to turn on an effect to improve the quality or look of a game.
        /// Call ReinitialiseThrusterEffects() after calling 1 or more of these methods.
        /// If you wish to enable or start a thruster, it is more likely you want to use EnableShip()
        /// or EnableShipMovement().
        /// WARNING: This will generate Garbage so use sparingly.
        /// </summary>
        /// <param name="effectNameContains"></param>
        public void EnableThrusterEffects(string effectNameContains)
        {
            arrayLength = thrusterEffectObjects == null ? 0 : thrusterEffectObjects.Length;

            // Look through all the thrusters for effects parent gameobjects
            for (componentIndex = 0; componentIndex < arrayLength; componentIndex++)
            {
                GameObject thrusterEffectsGO = thrusterEffectObjects[componentIndex];
                if (thrusterEffectsGO != null)
                {
                    // If the top level effects gameobject contains the string, then disable it.
                    if (thrusterEffectsGO.name.Contains(effectNameContains)) { thrusterEffectsGO.SetActive(true); }
                    else
                    {
                        // Search through the child objects including inactive transforms
                        Transform[] childTrfms = thrusterEffectsGO.GetComponentsInChildren<Transform>(true);
                        int numChildren = childTrfms == null ? 0 : childTrfms.Length;
                        for (int t = 0; t < numChildren; t++)
                        {
                            if (childTrfms[t] != null && childTrfms[t].name.Contains(effectNameContains))
                            {
                                childTrfms[t].gameObject.SetActive(true);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Disable all the thruster effects where the gameobject contains the specified string.
        /// Typically used to turn off an effect to reduce the performance overhead of running it.
        /// Call ReinitialiseThrusterEffects() after calling 1 or more of these methods.
        /// If you wish to pause or stop a thruster, it is more likely you want to use DisableShip()
        /// or DisableShipMovement().
        /// WARNING: This will generate Garbage so use sparingly.
        /// </summary>
        /// <param name="effectNameContains"></param>
        public void DisableThrusterEffects (string effectNameContains)
        {
            arrayLength = thrusterEffectObjects == null ? 0 : thrusterEffectObjects.Length;

            // Look through all the thrusters for effects parent gameobjects
            for (componentIndex = 0; componentIndex < arrayLength; componentIndex++)
            {
                GameObject thrusterEffectsGO = thrusterEffectObjects[componentIndex];
                if (thrusterEffectsGO != null)
                {
                    // If the top level effects gameobject contains the string, then disable it.
                    if (thrusterEffectsGO.name.Contains(effectNameContains)) { thrusterEffectsGO.SetActive(false); }
                    else
                    {
                        // Search through the child objects
                        Transform[] childTrfms = thrusterEffectsGO.GetComponentsInChildren<Transform>();
                        int numChildren = childTrfms == null ? 0 : childTrfms.Length;
                        for (int t = 0; t < numChildren; t++)
                        {
                            if (childTrfms[t] != null && childTrfms[t].name.Contains(effectNameContains))
                            {
                                childTrfms[t].gameObject.SetActive(false);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get a thruster based on the order it appears in the Thrusters tab in the ShipControlModule. Numbers begin at 1.
        /// </summary>
        /// <param name="thrusterNumber"></param>
        /// <returns></returns>
        public Thruster GetThruster (int thrusterNumber)
        {
            arrayLength = NumberOfThrusters;

            if (thrusterNumber > 0 && thrusterNumber <= arrayLength) { return shipInstance.thrusterList[thrusterNumber - 1]; }
            else { return null; }  
        }

        /// <summary>
        /// Attempt to get the first thruster with the given force input.
        /// Returns 0 if no matching thruster is found.
        /// </summary>
        /// <param name="forceInput"></param>
        /// <returns></returns>
        public int GetThrusterNumber (Thruster.ThrusterForceInput forceInput)
        {
            int firstThruster = 0;

            arrayLength = NumberOfThrusters;

            int forceInputInt = (int)forceInput;

            for (int thIdx = 0; thIdx < arrayLength; thIdx++)
            {
                Thruster thruster = shipInstance.thrusterList[thIdx];
                if (thruster != null && thruster.forceUse == forceInputInt)
                {
                    firstThruster = thIdx+1;
                    break;
                }
            }

            return firstThruster;
        }

        /// <summary>
        /// The supplied non-null list will be populated with matching thrusters from this ship
        /// </summary>
        /// <param name="forceInput"></param>
        /// <param name="selectedThrusterList"></param>
        /// <returns></returns>
        public bool GetThrustersByInputForce (Thruster.ThrusterForceInput forceInput, ref List<Thruster> selectedThrusterList)
        {
            bool hasMatchingThrusters = false;
            arrayLength = NumberOfThrusters;

            if (selectedThrusterList == null)
            {
                Debug.LogWarning("[ERROR] ShipControlModule (" + name + ") GetThrustersByInput(..) - selectedThrusterList cannot be null");
            }
            else
            {
                selectedThrusterList.Clear();
                int forceInputInt = (int)forceInput;

                for (int thIdx = 0; thIdx < arrayLength; thIdx++)
                {
                    Thruster thruster = shipInstance.thrusterList[thIdx];
                    if (thruster != null && thruster.forceUse == forceInputInt)
                    {
                        selectedThrusterList.Add(thruster);
                        hasMatchingThrusters = true;
                    }
                }
            }

            return hasMatchingThrusters;
        }

        /// <summary>
        /// Get the current audio volume for a given thruster. Numbers begin at 1.
        /// </summary>
        /// <param name="thrusterNumber"></param>
        /// <returns></returns>
        public float GetThrusterAudioVolume (int thrusterNumber)
        {
            float thrusterAudioVolume = 0f;

            if (isInitialised && thrusterNumber > 0 && thrusterNumber <= (thrusterEffects == null ? 0 : thrusterEffects.Length))
            {
                if (thrusterEffects[thrusterNumber - 1] != null)
                {
                    thrusterAudioVolume = thrusterEffects[thrusterNumber - 1].currentThrusterAudioVolume;
                }
            }

            return thrusterAudioVolume;
        }

        /// <summary>
        /// Get the parent gameobject for a given thruster effects. Numbers begin at 1.
        /// This is the Effects Object that appears in the inspector of a thruster (if any)
        /// </summary>
        /// <param name="thrusterNumber"></param>
        /// <returns></returns>
        public GameObject GetThrusterEffectsObject (int thrusterNumber)
        {
            if (isInitialised && thrusterNumber > 0 && thrusterNumber <= (thrusterEffectObjects == null ? 0 : thrusterEffectObjects.Length))
            {
                return thrusterEffectObjects[thrusterNumber - 1];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Set the maximum volume for a given thruster. Numbers begin at 1. Values should be between 0.0 and 1.0.
        /// </summary>
        /// <param name="thrusterNumber"></param>
        /// <param name="newMaxVolume"></param>
        public void SetThrusterMaxVolume (int thrusterNumber, float newMaxVolume)
        {
            if (isInitialised && thrusterNumber > 0 && thrusterNumber <= (thrusterEffects == null ? 0 : thrusterEffects.Length))
            {
                if (thrusterEffects[thrusterNumber - 1] != null)
                {
                    thrusterEffects[thrusterNumber - 1].SetMaxVolume(newMaxVolume);
                }
            }
        }

        /// <summary>
        /// Begin to shut down the thrusters. Optionally override the shutdown duration.
        /// </summary>
        public void ShutdownThrusterSystems (bool isInstantShutdown = false)
        {
            if (isInitialised)
            {
                shipInstance.ShutdownThrusterSystems(isInstantShutdown);
                UpdateThrusterFX();

                if (callbackOnThrusterSystemsChanged != null)
                {
                    callbackOnThrusterSystemsChanged.Invoke(this, isInstantShutdown ? 3 : 2);
                }
            }
        }

        /// <summary>
        /// Begin to bring the thrusters online. Optionally, override the startup duration.
        /// As soon as the systems begin to start up, shipInstance.isThrusterSystemsStarted will be true.
        /// </summary>
        /// <param name="isInstantStartup"></param>
        public void StartupThrusterSystems (bool isInstantStartup = false)
        {
            if (isInitialised && !shipInstance.isThrusterSystemsStarted)
            {
                shipInstance.StartupThrusterSystems(isInstantStartup);
                PauseOrResumeThrusters(true);
                UpdateThrusterFX();

                if (callbackOnThrusterSystemsChanged != null)
                {
                    callbackOnThrusterSystemsChanged.Invoke(this, isInstantStartup ? 1 : 0);
                }
            }
        }

        /// <summary>
        /// Immediately stop any boost that has been applied with AddBoost(..).
        /// </summary>
        public void StopBoost()
        {
            if (isInitialised) { shipInstance.StopBoost(); }
        }

        /// <summary>
        /// Stop all Thruster Effects on the ship
        /// </summary>
        public void StopThrusterEffects()
        {
            arrayLength = thrusterEffects == null ? 0 : thrusterEffects.Length;
            for (componentIndex = 0; componentIndex < arrayLength; componentIndex++)
            {
                if (thrusterEffects[componentIndex] != null)
                {
                    thrusterEffects[componentIndex].Stop();
                }
            }
        }

        #endregion

        #region Public API Methods - Third Party Integration

        #region Gravitas - Physics System

        /// <summary>
        /// Allows a third-party to change the rigid body on which the ship is acting to that which is passed in.
        /// NOTE: This is experimental not officially supported but may be of use to some customers
        /// </summary>
        /// <param name="rigidbody"></param>
        public void SetRigidBody (ref Rigidbody rigidbody)
        {
            rBody = rigidbody;
        }

        #endregion

        #endregion

        #region Public API Methods - Weapons

        /// <summary>
        /// If weapon is manual fire, attempt to manually fire
        /// this weapon based on the position in the Weapons list on
        /// the combat tab. Numbers begin at 1.
        /// This is safe, but less performant, version of
        /// shipInstance.FireWeaponIfReady(weapon).
        /// Call SetWeaponManualFire(..) once before use.
        /// These methods can be called from a PlayInputModule
        /// custom player input.
        /// </summary>
        /// <param name="weaponNumber"></param>
        public void FireWeaponIfReady (int weaponNumber)
        {
            if (isInitialised && weaponNumber > 0 && weaponNumber <= NumberOfWeapons)
            {
                shipInstance.FireWeaponIfReady(shipInstance.weaponList[weaponNumber - 1]);
            }
        }

        /// <summary>
        /// Set a weapon to manually fire. Call this once before
        /// calling FireWeaponIfReady(..).
        /// WeaponNumber is the position in the Weapons list on
        /// the combat tab. Numbers begin at 1.
        /// </summary>
        /// <param name="weaponNumber"></param>
        public void SetWeaponManualFire (int weaponNumber)
        {
            if (isInitialised && weaponNumber > 0 && weaponNumber <= NumberOfWeapons)
            {
                shipInstance.SetWeaponManualFire(shipInstance.weaponList[weaponNumber - 1]);
            }
        }

        /// <summary>
        /// Set the weapon to automatically fire and to be enabled
        /// for use with the AutoTargetingModule.
        /// WeaponNumber is the position in the Weapons list on
        /// the combat tab. Numbers begin at 1.
        /// </summary>
        /// <param name="weaponNumber"></param>
        public void SetWeaponAutoFire (int weaponNumber)
        {
            if (isInitialised && weaponNumber > 0 && weaponNumber <= NumberOfWeapons)
            {
                shipInstance.SetWeaponAutoFire(shipInstance.weaponList[weaponNumber - 1]);
            }
        }

        #endregion

        #region Public API Static Methods - General

        /// <summary>
        /// Is this non-trigger collider attached to a ship control module?
        /// </summary>
        /// <param name="collider"></param>
        /// <param name="shipControlModule"></param>
        /// <returns></returns>
        public static bool IsObjectAShip (Collider collider, out ShipControlModule shipControlModule)
        {
            bool isShip = false;
            shipControlModule = null;

            if (collider != null && !collider.isTrigger)
            {
                Rigidbody rBody = collider.attachedRigidbody;

                // The rigidbody should be a component on the same gameobject as the ShipControlModule.
                isShip = rBody != null && rBody.TryGetComponent(out shipControlModule);
            }

            return isShip;
        }

        #endregion
    }

    #region Public Structures

    /// <summary>
    /// Paramaters structure for CallbackOnHit (callback for Ship Control Module).
    /// We do not recommend keeping references to any fields within this structure.
    /// Use them in one frame, then discard them.
    /// </summary>
    public struct CallbackOnShipHitParameters
    {
        /// <summary>
        /// Hit information for the raycast hit against the ship.
        /// </summary>
        public RaycastHit hitInfo;
        /// <summary>
        /// Prefab for the projectile that hit the ship.
        /// </summary>
        public ProjectileModule projectilePrefab;
        /// <summary>
        /// Prefab for the beam that hit the ship
        /// </summary>
        public BeamModule beamPrefab;
        /// <summary>
        /// Amount of damage done by the projectile or beam.
        /// </summary>
        public float damageAmount;
        /// <summary>
        /// The squadron ID of the ship that fired the projectile or beam.
        /// </summary>
        public int sourceSquadronId;
        /// <summary>
        /// Ship that fired the projectile or beam, else 0
        /// </summary>
        public int sourceShipId;
    };

    #endregion
}
