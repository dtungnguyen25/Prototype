using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    /// <summary>
    /// A sample script that can run radar query and output the results to a materials main texture.
    /// This is useful when you want to display radar on a ship’s console.
    /// WARNING: This is only sample to demonstrate how API calls could be used in
    /// your own code. You should write your own version of this in your own namespace.
    /// You can use this component in your own game but be aware it may change in future releases.
    /// </summary>
    [AddComponentMenu("Sci-Fi Ship Controller/Samples/Radar to Material")]
    [HelpURL("https://scsmmedia.com/ssc-documentation")]
    public class SampleRadarToMaterial : MonoBehaviour
    {
        #region Public and Serialized Variables

        [Header("General")]
        public bool initialiseOnStart = false;

        [Tooltip("The mesh renderer with the material that contains the main texture that will be updated with the radar data")]
        public MeshRenderer meshRenderer = null;

        [Tooltip("The zero-based element of the material on the mesh renderer")]
        public int materialElement = 0;

        [Tooltip("The range, in metres, of the radar from the centre to the edges. Can be overridden at runtime using API methods.")]
        [SerializeField, Range(10f, 20000f)] private float displayRange = 2000f;

        [Tooltip("Uses 3D distances to determine range when querying the radar data.")]
        public bool is3DQueryEnabled = true;

        [Tooltip("The sort order of the results. None is the fastest option and has the lowest performance impact.")]
        public SSCRadarQuery.QuerySortOrder querySortOrder = SSCRadarQuery.QuerySortOrder.None;

        [Tooltip("An optional array of faction Ids to include in the radar query")]
        [SerializeField] private int[] factionsToInclude;

        [Tooltip("An optional array of faction Ids to exclude from the radar query")]
        [SerializeField] private int[] factionsToExclude;

        [Tooltip("An optional array of squadron Ids to include in the radar query")]
        [SerializeField] private int[] squadronsToInclude;

        [Tooltip("An optional array of squadron Ids to exclude from the radar query")]
        [SerializeField] private int[] squadronsToExclude;

        [Header("Movement")]

        /// <summary>
        /// The centre of the radar will move around with this ship. Use FollowShip(..) at runtime.
        /// </summary>
        [SerializeField] private ShipControlModule shipToFollow = null;

        /// <summary>
        /// The centre of the radar will move around with this gameobject.
        /// Use FollowGameObject(..) at runtime.
        /// </summary>
        [SerializeField] private GameObject gameobjectToFollow = null;

        /// <summary>
        /// The centre of the radar
        /// </summary>
        [SerializeField] private Vector3 centrePosition = Vector3.zero;

        [Header("Visuals")]

        [Tooltip("The overlay or rim width")]
        [SerializeField, Range(0, 50)] private int overlayWidth = 4;

        [Tooltip("The colour of the outer rim of the material.")]
        [SerializeField] private Color32 overlayColour = Color.blue;

        [Tooltip("The background colour of the material.")]
        [SerializeField] private Color32 backgroundColour = Color.black;

        [Tooltip("The colour of any blip that are considered as friendly. Determined by the factionId when available.")]
        [SerializeField] private Color32 blipFriendColour = Color.green;

        [Tooltip("The colour of any blip that are considered as hostile. Determined by the factionId when available.")]
        [SerializeField] private Color32 blipFoeColour = Color.red;

        [Tooltip("The colour of any blip that are considered as neutral. Determined by the factionId when available.")]
        [SerializeField] private Color32 blipNeutralColour = Color.white;

        [Tooltip("The width (and height) of the texture that is created at runtime for the material. Should be a power of 2.")]
        [SerializeField, Range(64, 4096)] private int textureWidth = 256;

        #endregion

        #region Public Properties


        public bool IsInitialised { get { return isInitialised; } }

        /// <summary>
        /// The number of results returned in the last query.
        /// </summary>
        public int ResultCount { get; private set; }

        #endregion

        #region Public Static Variables

        #endregion

        #region Private Variables - General

        private bool isInitialised = false;

        private bool isRadarTexAvailable = false;

        [System.NonSerialized] private SSCRadar sscRadar = null;
        private SSCRadarQuery sscRadarQuery = null;
        private SSCRadarDisplay sscRadarDisplay = new SSCRadarDisplay();
        [System.NonSerialized] private List<SSCRadarBlip> sscRadarResultsList = null;


        private bool isURP = false;
        private bool isHDRP = false;
        private string mainTex = "_MainTex";

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

        #region Update Methods

        void Update()
        {
            if (isInitialised && isRadarTexAvailable && sscRadar.IsInitialised)
            {
                if (sscRadarDisplay.isFollowShip)
                {
                    if (shipToFollow != null && shipToFollow.IsInitialised)
                    {
                        centrePosition = shipToFollow.shipInstance.TransformPosition;
                    }
                }
                else if (sscRadarDisplay.isFollowGameObject)
                {
                    if (gameobjectToFollow != null) { centrePosition = gameobjectToFollow.transform.position; }
                }

                // Run the query
                sscRadarQuery.centrePosition = centrePosition;
                sscRadarQuery.range = displayRange;
                sscRadarQuery.is3DQueryEnabled = is3DQueryEnabled;
                sscRadarQuery.querySortOrder = querySortOrder;

                sscRadar.GetRadarResults(sscRadarQuery, sscRadarResultsList);

                if (sscRadarDisplay.radarTex == null)
                {
                    Debug.LogWarning("[ERROR] radarTex is null");
                }
                else
                {
                    sscRadar.DrawTexture(sscRadarDisplay, sscRadarQuery, sscRadarResultsList, false);
                }
            }
        }

        #endregion

        #region Private and Internal Methods - General

        /// <summary>
        /// Check to see if the mesh render has a valid material and texture
        /// </summary>
        /// <param name="showErrors"></param>
        /// <returns></returns>
        private bool CheckRadarTexture(bool showErrors)
        {
            bool isValid = false;

            if (meshRenderer == null)
            {
                sscRadarDisplay.radarTex = null;
                if (showErrors) { Debug.LogWarning("[ERROR] SampleRadarToMaterial on " + name + " - the mesh renderer is null"); }
            }
            else
            {
                Material[] mats = meshRenderer.materials;

                if (mats == null || materialElement >= mats.Length)
                {
                    sscRadarDisplay.radarTex = null;
                    if (showErrors) { Debug.LogWarning("[ERROR] SampleRadarToMaterial on " + name + " - the materialElement (" + materialElement + ") is invalid."); }
                }
                else
                {
                    // Create a new texture so that it is unique to this instance of the material
                    sscRadarDisplay.radarTex = SSCUtils.CreateTexture(textureWidth, textureWidth, Color.white, true);

                    if (!sscRadarDisplay.radarTex.isReadable)
                    {  
                        if (showErrors) { Debug.LogWarning("[ERROR] SampleRadarToMaterial on " + name + " - the materialElement (" + materialElement + ") material (" + (mats[materialElement].name + ") texture (" + sscRadarDisplay.radarTex.name + ") is not readable")); }
                        sscRadarDisplay.radarTex = null;
                    }
                    else
                    {
                        mats[materialElement].SetTexture(mainTex, sscRadarDisplay.radarTex);

                        sscRadarDisplay.radarTex.name = meshRenderer.name;

                        isValid = true;
                    }
                }
            }

            return isValid;
        }

        private void RefreshQuery()
        {
            sscRadarQuery = new SSCRadarQuery();

            sscRadarQuery.factionId = SSCRadarQuery.IGNOREFACTION;
            SetFactionsToInclude(factionsToInclude);
            SetFactionsToExclude(factionsToExclude);
            SetSquadronsToInclude(squadronsToInclude);
            SetSquadronsToExclude(squadronsToExclude);

            if (shipToFollow != null)
            {
                FollowShip(shipToFollow);
            }
            else if (gameobjectToFollow != null)
            {
                FollowGameObject(gameobjectToFollow);
            }
            // If the ship or gameobject is not set but the script is
            // attached to a ship, follow the attached ship.
            else if (TryGetComponent(out shipToFollow))
            {
                FollowShip(shipToFollow);
            }
        }

        #endregion

        #region Events

        #endregion

        #region Public API Methods - General

        /// <summary>
        /// The centre of the radar will follow the ship.
        /// </summary>
        /// <param name="shipControlModule"></param>
        public void FollowShip (ShipControlModule shipControlModule)
        {
            shipToFollow = shipControlModule;
            sscRadarDisplay.isFollowShip = shipToFollow != null;
            sscRadarDisplay.shipToFollow = shipToFollow;
            if (sscRadarDisplay.isFollowShip)
            {
                gameobjectToFollow = null;
                sscRadarDisplay.isFollowGameObject = false;
                sscRadarDisplay.gameobjectToFollow = null;
            }
        }

        /// <summary>
        /// The centre of the radar will follow the gameobject.
        /// </summary>
        /// <param name="gameobject"></param>
        public void FollowGameObject (GameObject gameobject)
        {
            gameobjectToFollow = gameobject;
            sscRadarDisplay.isFollowGameObject = gameobjectToFollow != null;
            sscRadarDisplay.gameobjectToFollow = gameobjectToFollow;
            if (sscRadarDisplay.isFollowGameObject)
            {
                shipToFollow = null;
                sscRadarDisplay.isFollowShip = false;
                sscRadarDisplay.shipToFollow = null;
            }
        }

        /// <summary>
        /// Attempt to initialise the component.
        /// </summary>
        public void Initialise()
        {
            if (isInitialised) { return; }

            isURP = SSCUtils.IsURP(false);
            if (!isURP) { isHDRP = SSCUtils.IsHDRP(false); }

            if (isURP || isHDRP) { mainTex = "_BaseMap"; }

            RefreshRadarMaterialStatus(true);

            if (isRadarTexAvailable)
            {
                sscRadar = SSCRadar.GetOrCreateRadar(gameObject.scene.handle);

                isInitialised = true;

                RefreshColours();

                SetOverlayWidth(overlayWidth);
            }
        }

        /// <summary>
        /// Refresh or update the colours used to render the material
        /// </summary>
        public void RefreshColours()
        {
            if (isInitialised)
            {
                sscRadarDisplay.backgroundColour = backgroundColour;
                sscRadarDisplay.overlayColour = overlayColour;
                sscRadarDisplay.blipFoeColour = blipFoeColour;
                sscRadarDisplay.blipFriendColour = blipFriendColour;
                sscRadarDisplay.blipNeutralColour = blipNeutralColour;
            }
        }

        /// <summary>
        /// Call at runtime if material is swapped or made null
        /// </summary>
        public void RefreshRadarMaterialStatus(bool showErrors)
        {
            isRadarTexAvailable = CheckRadarTexture(showErrors);

            if (isRadarTexAvailable)
            {
                RefreshQuery();
               
                sscRadarResultsList = new List<SSCRadarBlip>(20);
                // There are currently no results.
                ResultCount = 0;
                sscRadarDisplay.uiTexWidth = sscRadarDisplay.radarTex.width;
                sscRadarDisplay.uiTexHeight = sscRadarDisplay.radarTex.height;
                sscRadarDisplay.uiTexCentreX = Mathf.CeilToInt(sscRadarDisplay.uiTexWidth * 0.5f);
                sscRadarDisplay.uiTexCentreY = Mathf.CeilToInt(sscRadarDisplay.uiTexHeight * 0.5f);
            }
        }

        /// <summary>
        /// Set the world space position of the radar system. Use when
        /// you want the radar system to "move" with gameobject.
        /// </summary>
        /// <param name="centre"></param>
        /// <param name="range"></param>
        public void SetDisplay(GameObject centre, float range)
        {
            gameobjectToFollow = centre;
            displayRange = range;
        }

        /// <summary>
        /// Set the world space position of the radar system. Use when
        /// you want the radar system to be fixed to a given location or
        /// will move infrequently.
        /// </summary>
        /// <param name="centre"></param>
        /// <param name="range"></param>
        public void SetDisplay(Vector3 centre, float range)
        {
            gameobjectToFollow = null;
            centrePosition = centre;
            displayRange = range;
        }

        /// <summary>
        /// Set an array of factionIds to exclude from the radar query
        /// </summary>
        /// <param name="factionIds"></param>
        public void SetFactionsToExclude (int[] factionIds)
        {
            factionsToExclude = factionIds;
            sscRadarQuery.factionsToExclude = factionsToExclude;
        }

        /// <summary>
        /// Set an array of factionIds to include in the radar query
        /// </summary>
        /// <param name="factionIds"></param>
        public void SetFactionsToInclude (int[] factionIds)
        {
            factionsToInclude = factionIds;
            sscRadarQuery.factionsToInclude = factionsToInclude;
        }

        /// <summary>
        /// Set the overlay or rim width
        /// </summary>
        /// <param name="newRimWidth"></param>
        public void SetOverlayWidth (int newOverlayWidth)
        {
            overlayWidth = newOverlayWidth;
            if (isInitialised)
            {
                sscRadarDisplay.uiRimWidth = overlayWidth;
            }
        }

        /// <summary>
        /// Set an array of SquadronIds to exclude from the radar query
        /// </summary>
        /// <param name="squadronIds"></param>
        public void SetSquadronsToExclude (int[] squadronIds)
        {
            squadronsToExclude = squadronIds;
            sscRadarQuery.squadronsToExclude = squadronsToExclude;
        }

        /// <summary>
        /// Set an array of SquadronIds to include in the radar query
        /// </summary>
        /// <param name="squadronIds"></param>
        public void SetSquadronsToInclude (int[] squadronIds)
        {
            squadronsToInclude = squadronIds;
            sscRadarQuery.squadronsToInclude = squadronsToInclude;
        }

        #endregion

    }
}