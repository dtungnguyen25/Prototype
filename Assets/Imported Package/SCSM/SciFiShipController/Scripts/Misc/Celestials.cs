#define _DEBUG_STARS_HDRP
using UnityEngine;
using System.Collections.Generic;
#if SSC_URP
using UnityEngine.Rendering.Universal;
#elif SSC_HDRP
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
#endif

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    /// <summary>
    /// Render stars in the sky. Add distant planets (BiRP, URP only).
    /// URP requires Unity 2020.3.25+ and URP 10.7.0+.
    /// HDRP requires Unity 2022.3+ and HDRP 14.x+.
    /// BiRP and URP only features:
    /// - Supports one or two cameras.
    /// - Distant planets
    /// - To sync star rotation externally, set timing type to manual and
    /// call Celestials.SyncStars() after your camera has rotated.
    /// </summary>
    [DisallowMultipleComponent]
    public class Celestials : MonoBehaviour
    {
        // To change:
        // 1. Delete the existing (default) Unity layer of SSC Celestials
        // 2. Change celestialsUnityLayer here
        // 3. Save this file
        // 4. Go back to the Unity editor and wait for it to recompile scripts
        // This will need to be done each time you import a new version of SSC.
        #if LANDSCAPE_BUILDER
        public static readonly int celestialsUnityLayer = 26;
        #else
        public static readonly int celestialsUnityLayer = 25;
        #endif

        #region Enumerations

        public enum EnvAmbientSource
        {
            Colour = 0,
            Gradient = 1,
            Skybox = 2
        }

        public enum TimingType
        {
            Auto = 0,
            Manual = 1
        }

        #endregion

        #region Public Variables - General
        [Tooltip("The main camera")]
        public Camera camera1;

        [Tooltip("An optional second game camera")]
        public Camera camera2;

        [Tooltip("The colour of the night sky")]
        public Color nightSkyColour = new Color(21f / 255f, 21f / 255f, 21f / 255f, 1f);

        [Tooltip("The material to be used to create each star")]
        public Material starMaterial;

        [Tooltip("A low poly mesh to be used to create each star")]
        public Mesh starMesh;

        [Tooltip("The number of stars to create")]
        public int numberOfStars = 1000;

        public float starSize = 2f;

        [Tooltip("Attempt to make a more randomised position for stars - especially for RefreshCelestials()")]
        public bool isStarfieldRandom = false;

        [Tooltip("If enabled, the Initialise () will be called as soon as Awake () runs. This should be disabled if you are instantiating through code.")]
        public bool initialiseOnAwake = true;

        /// <summary>
        /// The stars finish at the horizon rather than filling the whole screen.
        /// </summary>
        public bool useHorizon = true;

        public EnvAmbientSource envAmbientSource = EnvAmbientSource.Colour;

        [Tooltip("By default, the ambient sky colour will be set to the nightSkyColour")]
        public bool overrideAmbientColour = false;

        [Tooltip("If overriding the ambient colour, this is the ambient sky colour")]
        public Color ambientSkyColour = new Color(21f / 255f, 21f / 255f, 21f / 255f, 1f);

        [Tooltip("Near clip plane for the celestial camera(s). Reduce if planets start being clipped by camera")]
        [Range(0.0001f, 0.1f)] public float nearClipPlane = 0.1f;

        [Tooltip("Create the stars but hide them immediately")]
        public bool isCreateHiddenStars = false;

        [Tooltip("Create the planets that are marked as hidden")]
        public bool isCreateHiddenPlanets = false;

        [Tooltip("List of planet or celestial objects")]
        public List<SSCCelestial> celestialList = new List<SSCCelestial>();

        #endregion

        #region Public Variables - HDRP
        #if SSC_HDRP
        /// <summary>
        /// The HDRP volume used in the scene to configure the HDRi sky
        /// </summary>
        public Volume skyVolume;

        #endif
        #endregion

        #region Public Properties

        /// <summary>
        /// Get a reference to the first celestials camera
        /// </summary>
        public Camera CelestialsCamera1 { get { return celestialsCamera1; } }

        /// <summary>
        /// Get a reference to the second celestials camera
        /// </summary>
        public Camera CelestialsCamera2 { get { return celestialsCamera2; } }

        /// <summary>
        /// The amount the distance between the celestial (BiRP, URP) or camera1 (HDRP)
        /// and the planet has been scaled.
        /// </summary>
        public float DistanceScale { get { return distanceScale; } }

        public bool IsInitialised { get { return isInitialised; } }

        /// <summary>
        /// By default, stars are randomised in a consistent way to recreate reproduceable results.
        /// However, they can be further randomised to make the starfield to be more random each time
        /// it is recreated during the same session.
        /// </summary>
        public bool IsStarfieldRandom { get { return isStarfieldRandom; } set { isStarfieldRandom = value; } }

        /// <summary>
        /// Set to manual if you want to manually call UpdateCelestialsRotation() in your own code.
        /// </summary>
        public TimingType CelestialsTimingType { get { return timingType; } set { SetTimingType(value); } }

        /// <summary>
        /// Get or set if the stars will use the horizon or not. If useHorizon is changed after celestials
        /// have been created, they will be rebuilt to reflect the new setting.
        /// </summary>
        public bool UseHorizon { get { return useHorizon; } set { CheckChangeHorizon(value); } }

        #endregion

        #region Public Static Variables

        public static Celestials CelestialsInstance { get; private set; }

        public static readonly int timingTypeIntAuto = (int)TimingType.Auto;
        public static readonly int timingTypeIntManual = (int)TimingType.Manual;

        #endregion

        #region Protected variables

        [Tooltip("Set to manual if you want to manually call UpdateCelestialsRotation() in your own code.")]
        [SerializeField] protected TimingType timingType = TimingType.Auto;

        [System.NonSerialized] protected Camera celestialsCamera1 = null;
        [System.NonSerialized] protected Camera celestialsCamera2 = null;
        protected bool isInitialised = false;
        [System.NonSerialized] protected CombineInstance[] combineInstances;
        protected bool isCamera2Initialised = false;
        protected SSCRandom sscRandom = null;
        [System.NonSerialized] protected MeshRenderer celestialMeshRenderer = null;
        [System.NonSerialized] protected MeshRenderer starMRenderer = null;

        [System.NonSerialized] protected Transform planetsTrfm = null;

        protected float minCelestialCameraDistance = 0.2f;
        protected int timingTypeInt = (int)TimingType.Auto;

        #if SSC_HDRP
        protected float distanceScale = 1000f;
        #else
        protected float distanceScale = 1f;
        #endif

        #endregion

        #region Protected Variables - HDRP
        #if SSC_HDRP
        /// <summary>
        /// The stars colour range from
        /// </summary>
        [SerializeField] protected Color colourFrom = Color.white;

        /// <summary>
        /// The stars colour range to
        /// </summary>
        [SerializeField] protected Color colourTo = Color.white;

        /// <summary>
        /// The amount of fade to apply to the stars
        /// </summary>
        [SerializeField, Range(0f, 1f)] protected float fadeStars = 0.6f;

        /// <summary>
        /// The density of the stars
        /// </summary>
        [SerializeField, Range(0f, 1f)] protected float density = 0.4f;

        /// <summary>
        /// The starfield to be displayed
        /// </summary>
        [SerializeField, Range(0f, 1f)] protected float starfield = 0.5f;

        /// <summary>
        /// The normalised position of the horizon on the y-axis
        /// </summary>
        [SerializeField, Range(0f, 1f)] protected float horizon = 1f;

        protected HDRISky hdriSkyOverride;
        protected Material skyMaterial;
        protected CustomRenderTexture tempCustomRT;
        protected int matHasHorizonId = 0;
        protected int matHorizonId = 0;
        protected int matColourFromId = 0;
        protected int matColourToId = 0;
        protected int matDensityId = 0;
        protected int matStarfieldId = 0;
        protected int matFadeStarsId = 0;

        #endif
        #endregion

        #region Initialisation Methods

        // Use this for initialization
        void Awake()
        {
            if (initialiseOnAwake) { Initialise(); }            
        }

        /// <summary>
        /// Initialise the celestials camera(s).
        /// </summary>
        public void Initialise()
        {
            isInitialised = false;

            #if SSC_HDRP
            
            if (camera1 == null)
            { 
                Debug.LogWarning("SSC Celestials - the (main) Camera1 is not set on " + name + " in scene " + gameObject.scene.name);
            }
            else if (skyVolume == null)
            {
                Debug.LogWarning("[ERROR] Celestials.Initialise - the HDRP volume for the HDRI Sky is not set on " + name + " in scene " + gameObject.scene.name);
            }
            else if (skyVolume.profile == null)
            {
                Debug.LogWarning("[ERROR] Celestials.Initialise - the HDRP profile for the HDRI Sky is not set on " + skyVolume.gameObject.name + " for " + name + " in scene " + gameObject.scene.name);
            }
            else if (!skyVolume.profile.TryGet(out hdriSkyOverride))
            {
                Debug.LogWarning("[ERROR] Celestials.Initialise - the HDRP HDRI Sky component is not found on " + skyVolume.gameObject.name + " for " + name + " in scene " + gameObject.scene.name);
            }
            else if (hdriSkyOverride.hdriSky == null)
            {
                Debug.LogWarning("[ERROR] Celestials.Initialise - the HDRP HDRI Sky cubemap on " + skyVolume.gameObject.name + " is not set for " + name + " in scene " + gameObject.scene.name);
            }
            else
            {
                //CubemapParameter cubeMapParm = hdriSkyOverride.hdriSky;

                #if UNITY_2021_3_OR_NEWER
                // Check if there is a custom render texture used for the cubemap
                CustomRenderTexture customRT = hdriSkyOverride.hdriSky.value as CustomRenderTexture;
                #else
                Debug.LogWarning("[ERROR] Celestials.Initialise - celestials for HDRP requires HDRP 14.x or newer (Unity 2022.3+)");
                CustomRenderTexture customRT = null;
                #endif

                if (customRT == null)
                {
                    Debug.LogWarning("[ERROR] Celestials.Initialise - the HDRP HDRI Sky component on " + skyVolume.gameObject.name + " does not have a Custom Render Texture cubemap for " + name + " in scene " + gameObject.scene.name);
                }
                else
                {
                    skyMaterial = customRT.material;

                    // Debug at the top of the script will allow you to directly update the material in the project
                    // and/or modify and save the shadergraph at runtime to see the effect in the game view.
                    #if UNITY_2021_3_OR_NEWER && !DEBUG_STARS_HDRP
                    if (skyMaterial != null)
                    {
                        skyMaterial = new Material(skyMaterial);

                        // NOTE: CustomRenderTexture.GetTemporary(customRT.descritor) only creates a rendertexture, not a custom RT.

                        tempCustomRT = new CustomRenderTexture(customRT.width, customRT.height, customRT.format)
                        {
                            dimension = customRT.dimension,
                            descriptor = customRT.descriptor,
                            depth = customRT.depth,
                            antiAliasing = customRT.antiAliasing,
                            format = customRT.format,
                            depthStencilFormat = customRT.depthStencilFormat,
                            initializationMode = customRT.initializationMode,
                            initializationSource = customRT.initializationSource,
                            initializationMaterial = skyMaterial,
                            material = skyMaterial,
                            shaderPass = customRT.shaderPass,
                            updateMode = customRT.updateMode,
                            updatePeriod = customRT.updatePeriod,
                            name = customRT.name + " (copy)"
                        };

                        tempCustomRT.Create();

                        if (tempCustomRT.IsCreated())
                        {
                            hdriSkyOverride.hdriSky.value = tempCustomRT;
                        }                       
                    }
                    #endif

                    if (skyMaterial == null)
                    {
                        Debug.LogWarning("[ERROR] Celestials.Initialise - the HDRP HDRI Sky volume Cubemap Custom Render Texture does not have a Material on " + customRT.name + " for " + name + " in scene " + gameObject.scene.name);
                    }
                    else if (!SSCUtils.GetShaderPropertyId(skyMaterial, "_Horizon", out matHorizonId))
                    {
                        Debug.LogWarning("[ERROR] Celestials.Initialise - the HDRP HDRI Sky volume Cubemap Custom Render Texture (" + customRT.name + ") material (" + skyMaterial.name + ") does not have a property called Horizon for " + name + " in scene " + gameObject.scene.name);
                    }
                    else if (!SSCUtils.GetShaderPropertyId(skyMaterial, "_HasHorizon", out matHasHorizonId))
                    {
                        Debug.LogWarning("[ERROR] Celestials.Initialise - the HDRP HDRI Sky volume Cubemap Custom Render Texture (" + customRT.name + ") material (" + skyMaterial.name + ") does not have a property called HasHorizon for " + name + " in scene " + gameObject.scene.name);
                    }
                    else if (!SSCUtils.GetShaderPropertyId(skyMaterial, "_ColourFrom", out matColourFromId))
                    {
                        Debug.LogWarning("[ERROR] Celestials.Initialise - the HDRP HDRI Sky volume Cubemap Custom Render Texture (" + customRT.name + ") material (" + skyMaterial.name + ") does not have a property called ColourFrom for " + name + " in scene " + gameObject.scene.name);
                    }
                    else if (!SSCUtils.GetShaderPropertyId(skyMaterial, "_ColourTo", out matColourToId))
                    {
                        Debug.LogWarning("[ERROR] Celestials.Initialise - the HDRP HDRI Sky volume Cubemap Custom Render Texture (" + customRT.name + ") material (" + skyMaterial.name + ") does not have a property called ColourTo for " + name + " in scene " + gameObject.scene.name);
                    }
                    else if (!SSCUtils.GetShaderPropertyId(skyMaterial, "_Density", out matDensityId))
                    {
                        Debug.LogWarning("[ERROR] Celestials.Initialise - the HDRP HDRI Sky volume Cubemap Custom Render Texture (" + customRT.name + ") material (" + skyMaterial.name + ") does not have a property called Density for " + name + " in scene " + gameObject.scene.name);
                    }
                    else if (!SSCUtils.GetShaderPropertyId(skyMaterial, "_Starfield", out matStarfieldId))
                    {
                        Debug.LogWarning("[ERROR] Celestials.Initialise - the HDRP HDRI Sky volume Cubemap Custom Render Texture (" + customRT.name + ") material (" + skyMaterial.name + ") does not have a property called Starfield for " + name + " in scene " + gameObject.scene.name);
                    }
                    else if (!SSCUtils.GetShaderPropertyId(skyMaterial, "_FadeStars", out matFadeStarsId))
                    {
                        Debug.LogWarning("[ERROR] Celestials.Initialise - the HDRP HDRI Sky volume Cubemap Custom Render Texture (" + customRT.name + ") material (" + skyMaterial.name + ") does not have a property called FadeStars for " + name + " in scene " + gameObject.scene.name);
                    }
                    #if !DEBUG_STARS_HDRP
                    else if (tempCustomRT == null || !tempCustomRT.IsCreated())
                    {
                        Debug.LogWarning("[ERROR] Celestials.Initialise - the HDRP HDRI Sky volume could not create temporary custom render texture from (" + customRT.name + " for " + name + " in scene " + gameObject.scene.name);
                    }
                    #endif
                    else
                    {
                        //Debug.Log("[DEBUG] customRT: " + customRT.name);
                        BuildCelestials();

                        isInitialised = true;

                        if (isCreateHiddenStars) { HideStars(); }
                        else { SetFadeStars(fadeStars); }

                        SetStarfield(starfield);
                        SetStarDensity(density);
                        SetHorizon(horizon);
                        if (useHorizon) { EnableHorizon(); }
                        else { DisableHorizon(); }

                        SetStarColourFrom(colourFrom);
                        SetStarColourTo(colourTo);
                    }
                }
            }

            #else

            // Only add celestials camera if it doesn't already exist
            Transform celestialCamera1Trm = GetorCreateCamera("Celestials Camera 1", out celestialsCamera1);

            // Configure the celestials camera 1
            if (celestialCamera1Trm != null)
            {
                if (camera1 == null) { Debug.LogWarning("SSC Celestials - the (main) Camera1 is not set"); }
                else if (celestialsCamera1 == null) { Debug.LogWarning("SSC Celestials - did not create Celestials Camera 1"); }
                else if (IsVerifyStarSettings())
                {
                    ConfigCameras(celestialsCamera1, camera1, -100f);
                    BuildCelestials();

                    if (camera2 != null) { InitialiseCamera2(); }

                    UpdateRenderSettings();

                    timingTypeInt = (int)timingType;

                    // Only update the instance if it is not already set.
                    // With additive scenes, this may become a problem. If so, we'll
                    // need to come up with some other rules or methods.
                    // For example, this doesn't work with SSC X Pack 2.

                    if (CelestialsInstance == null) { CelestialsInstance = this; }

                    isInitialised = true;
                }
            }
            #endif
        }

        #endregion

        #region Events

        private void OnDestroy()
        {
            #if SSC_HDRP
            if (tempCustomRT != null)
            {
                if (tempCustomRT.IsCreated())
                {
                    tempCustomRT.DiscardContents();
                    tempCustomRT.Release();
                }
                tempCustomRT = null;
            }
            #endif

            if (CelestialsInstance != null && CelestialsInstance == this)
            {
                CelestialsInstance = null;
            }
        }

        #endregion

        #region Update Methods

        #if !SSC_HDRP
        // Called during the physics update loop
        // Added in SSC 1.3.7 to make more compatible with
        // Sticky3D character when walking and looking left/right
        void FixedUpdate()
        {
            if (isInitialised && timingTypeInt == timingTypeIntAuto)
            {
                UpdateCelestialsRotation();
            }
        }
        #endif

        #if !SSC_HDRP
        /// <summary>
        /// Changed from Update to LateUpdate() in SSC 1.3.3
        /// to overcome issue in a build when using S3D in first
        /// person and character movement in FixedUpdate.
        /// </summary>
        private void LateUpdate()
        {
            if (isInitialised && timingTypeInt == timingTypeIntAuto)
            {
                UpdateCelestialsRotation();
            }
        }
        #endif

        #endregion

        #region Protected Methods

        protected void BuildCelestials()
        {
            DestroyStarGameObject();
            CreateOrConfigureRandom();

            #if SSC_HDRP
            #if UNITY_2021_3_OR_NEWER
            // For consistency, create the same hierachy as BiRP and URP
            GameObject starsGameObject = CreateStarsGameObject();
            if (starsGameObject != null)
            {
                CreatePlanets(starsGameObject);
            }
            #endif
            #else            
            GameObject starsGameObject = CreateStars();

            if (starsGameObject != null)
            {
                if (isCreateHiddenStars && starMRenderer != null) { starMRenderer.enabled = false; }
                CreatePlanets(starsGameObject);
            }
            #endif
        }

        /// <summary>
        /// Check if we need to refresh the stars
        /// </summary>
        /// <param name="isHorizon"></param>
        protected void CheckChangeHorizon (bool isHorizon)
        {
            if (isInitialised)
            {
                if (isHorizon != useHorizon)
                {
                    useHorizon = isHorizon;
                    RefreshCelestials();
                }
            }
            else
            {
                useHorizon = isHorizon;
            }
        }

        /// <summary>
        /// Calculate where the celestial (planet) should be placed relative to the celestials camera(s).
        /// </summary>
        /// <param name="sscCelestial"></param>
        /// <returns></returns>
        protected Vector3 CalcCelestialPosition(SSCCelestial sscCelestial)
        {
            Vector3 objectPosition = sscCelestial.celestialToDirection * (minCelestialCameraDistance + sscCelestial.currentCelestialDistance);

            if (useHorizon && objectPosition.y < 0f) { objectPosition.y = -objectPosition.y; }

            // Cater for the celestials gameobject not being at 0,0,0
            objectPosition += transform.position;

            #if SSC_HDRP
            // In U2021.3+ HDRP the planet is offset from the display camera
            objectPosition += camera1.transform.position;
            #endif

            return objectPosition;
        }

        /// <summary>
        /// Configure an instance of SSCRandom, if it hasn't
        /// already been done in this session.
        /// </summary>
        protected void CreateOrConfigureRandom()
        {
            if (sscRandom == null)
            {
                sscRandom = new SSCRandom();
                sscRandom.SetSeed(821997);
            }
        }

        /// <summary>
        /// Create the stars mesh
        /// </summary>
        /// <returns></returns>
        protected GameObject CreateStars()
        {
            GameObject starMeshGameObject = null;

            if (starMesh != null)
            {
                starMeshGameObject = CreateStarsGameObject();
                MeshFilter starMFilter = starMeshGameObject.AddComponent<MeshFilter>();
                starMRenderer = starMeshGameObject.AddComponent<MeshRenderer>();

                // Get the number of verts per star mesh
                int starMeshVerts = starMesh.vertices == null ? 0 : starMesh.vertices.Length;
                int totalVerts = 0;

                starMRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                starMRenderer.receiveShadows = false;

                starMeshGameObject.layer = celestialsUnityLayer;
                UnityEngine.Random.InitState(0);

                if (isStarfieldRandom) { sscRandom.SetSeed((int)Time.realtimeSinceStartup); }

                combineInstances = new CombineInstance[numberOfStars];
                Vector3 starPos;
                for (int i = 0; i < combineInstances.Length; i++)
                {
                    // Attempt to create a more randomised layout for the stars
                    if (isStarfieldRandom)
                    {
                        starPos = UnityEngine.Random.onUnitSphere * sscRandom.Range(1f, 5f);
                    }
                    else
                    {
                        starPos = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(1f, 5f);
                    }
                    if (useHorizon && starPos.y < 0f) { starPos.y = -starPos.y; }
                    combineInstances[i].transform = Matrix4x4.TRS(starPos, Quaternion.identity, 0.001f * starSize * Vector3.one);
                    combineInstances[i].mesh = starMesh;
                    totalVerts += starMeshVerts;
                }

                starMFilter.sharedMesh = new Mesh();
                starMFilter.sharedMesh.name = "SSC Stars Mesh";
                // Check if there are more than 65535 verts
                if (totalVerts > ushort.MaxValue)
                {
                    starMFilter.sharedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                }
                starMFilter.sharedMesh.CombineMeshes(combineInstances);

                if (starMaterial != null) { starMRenderer.material = starMaterial; }

                starMeshGameObject.isStatic = true;
            }

            return starMeshGameObject;
        }

        /// <summary>
        /// Create the child stars gameobject
        /// </summary>
        /// <returns></returns>
        protected GameObject CreateStarsGameObject()
        {
            GameObject starsGameObject = new GameObject("SSC Stars");
            starsGameObject.transform.parent = transform;
            starsGameObject.transform.localPosition = Vector3.zero;
            starsGameObject.transform.localRotation = Quaternion.identity;
            starsGameObject.transform.localScale = Vector3.one;
            return starsGameObject;
        }

        /// <summary>
        /// Create a planet.
        /// Behaviour change in 1.3.7 Beta 6a (create hidden planets but disable them immediately).
        /// </summary>
        /// <returns></returns>
        protected Transform CreatePlanet(SSCCelestial sscCelestial)
        {
            Transform planetTrfm = null;

            if (sscCelestial != null)
            {
                CreateOrConfigureRandom();

                // BiRP and URP planets are rendered with the Celestials camera
                // and are positions pretty close to the camera and therefore scaled down.
                float scaleFactor = 0.01f;

                #if SSC_HDRP
                //scaleFactor = 10f;
                scaleFactor *= distanceScale;
                #else

                #endif

                // Validate the min/max size values
                sscCelestial.minSize = Mathf.Clamp(sscCelestial.minSize, 1, 20);
                sscCelestial.maxSize = Mathf.Clamp(sscCelestial.maxSize, sscCelestial.minSize, 20);

                sscCelestial.minDistance = Mathf.Clamp(sscCelestial.minDistance, 0f, 1f) * distanceScale;
                sscCelestial.maxDistance = Mathf.Clamp(sscCelestial.maxDistance, sscCelestial.minDistance, 1f) * distanceScale;

                // Get the random numbers before deciding to skip hidden (not created) planets.
                // This allows the other planet to still be rendered in their original locations.
                float planetScale = sscRandom.Range(sscCelestial.minSize, sscCelestial.maxSize) * scaleFactor;
                sscCelestial.currentCelestialDistance = Mathf.Clamp(sscRandom.Range(sscCelestial.minDistance, sscCelestial.maxDistance + 0.01f), sscCelestial.minDistance, sscCelestial.maxDistance);

                // The min distance from the camera is ~0.2, so let the user select a -1.0 - 1.0 range,
                // but always add on the minimum camera distance. This should mostly avoid the planet being
                // clipped by the camera.

                sscCelestial.celestialToDirection = sscCelestial.isRandomPosition ? UnityEngine.Random.onUnitSphere : new Vector3(sscCelestial.positionX, sscCelestial.positionY, sscCelestial.positionZ);

                // Normalise the direction
                if (!sscCelestial.isRandomPosition)
                {
                    if (sscCelestial.celestialToDirection.sqrMagnitude < Mathf.Epsilon)
                    {
                        sscCelestial.celestialToDirection = new Vector3(0f, 0f, 1f);
                    }

                    sscCelestial.celestialToDirection.Normalize();
                }

                Vector3 planetPos = CalcCelestialPosition(sscCelestial);

                if (!sscCelestial.isHidden || isCreateHiddenPlanets)
                {
                    GameObject planetGO = null;

                    if (sscCelestial.celestialMesh == null)
                    {
                        planetGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                        Collider planetCollider;
                        if (planetGO.TryGetComponent(out planetCollider))
                        {
                            #if UNITY_EDITOR
                            DestroyImmediate(planetCollider);
                            #else
                            Destroy(planetCollider);
                            #endif
                        }
                    }
                    else
                    {
                        planetGO = new GameObject(sscCelestial.name);

                        MeshFilter mFilter = planetGO.AddComponent<MeshFilter>();
                        MeshRenderer mRenderer = planetGO.AddComponent<MeshRenderer>();

                        mFilter.mesh = sscCelestial.celestialMesh;
                        mRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                        mRenderer.receiveShadows = true;
                    }

                    if (!string.IsNullOrEmpty(sscCelestial.name)) { planetGO.name = sscCelestial.name; }
                    else { planetGO.name = "Unknown"; }

                    planetGO.layer = celestialsUnityLayer;
                    planetTrfm = planetGO.transform;

                    // Store the transform for easy access
                    sscCelestial.celestialTfrm = planetTrfm;

                    planetTrfm.SetPositionAndRotation(planetPos, Quaternion.identity);
                    planetTrfm.localScale *= planetScale;

                    // If the user has supplied a material attempt to update the planet's material
                    if (sscCelestial.celestialMaterial != null)
                    {
                        if (planetGO.TryGetComponent(out celestialMeshRenderer))
                        {
                            celestialMeshRenderer.material = sscCelestial.celestialMaterial;
                        }
                    }

                    if (sscCelestial.isFaceCamera1 && camera1 != null)
                    {
                        // Planet (A) to look at camera (B). Direction = (B-A).normalized
                        Vector3 _lookVector = camera1.transform.position - planetPos;
                        if (_lookVector != Vector3.zero)
                        {
                            planetTrfm.rotation = Quaternion.LookRotation(_lookVector.normalized);
                        }
                    }
                    else
                    {
                        planetTrfm.rotation = Quaternion.Euler(sscCelestial.rotation);
                    }

                    if (sscCelestial.isHidden) { planetGO.SetActive(false); }
                }
            }

            return planetTrfm;
        }

        /// <summary>
        /// Attempt to create the planets as a child of the stars
        /// </summary>
        /// <param name="starsGameObject"></param>
        protected void CreatePlanets(GameObject starsGameObject)
        {
            if (starsGameObject != null)
            {
                GameObject planetsGameObject = new GameObject("SSC Planets");

                if (planetsGameObject != null)
                {
                    planetsTrfm = planetsGameObject.transform;

                    planetsTrfm.SetParent(starsGameObject.transform);
                    planetsTrfm.localPosition = Vector3.zero;
                    planetsTrfm.localRotation = Quaternion.identity;
                    planetsTrfm.localScale = Vector3.one;
                    planetsGameObject.layer = celestialsUnityLayer;

                    // Create from a list of planet prefabs
                    int numPlanets = celestialList == null ? 0 : celestialList.Count;

                    for (int cIdx = 0; cIdx < numPlanets; cIdx++)
                    {
                        Transform planet1Trfm = CreatePlanet(celestialList[cIdx]);

                        if (planet1Trfm != null)
                        {
                            planet1Trfm.SetParent(planetsTrfm);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Configure a celestial and display camera pair
        /// </summary>
        /// <param name="celestialsCamera"></param>
        /// <param name="displayCamera"></param>
        /// <param name="celestialsCameraDepth"></param>
        protected void ConfigCameras(Camera celestialsCamera, Camera displayCamera, float celestialsCameraDepth)
        {
            if (celestialsCamera != null && displayCamera != null)
            {              
                celestialsCamera.nearClipPlane = nearClipPlane;
                celestialsCamera.farClipPlane = 10f;
                celestialsCamera.depth = celestialsCameraDepth;
                celestialsCamera.clearFlags = envAmbientSource == EnvAmbientSource.Skybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                celestialsCamera.backgroundColor = nightSkyColour;
                celestialsCamera.cullingMask = 1 << celestialsUnityLayer;

                celestialsCamera.fieldOfView = displayCamera.fieldOfView;
                // Set the celestials camera to use the same monitor or "display" as the display camera
                celestialsCamera.targetDisplay = displayCamera.targetDisplay;

                // Automatically exclude celestials from display camera
                displayCamera.cullingMask &= ~(1 << celestialsUnityLayer);

                #if SSC_URP
                // Make the main camera an overlay camera (requires URP 7.3.1 or newer)
                var cameraDataCelestials = celestialsCamera.GetUniversalAdditionalCameraData();
                var cameraDataDisplay = displayCamera.GetUniversalAdditionalCameraData();
                if (cameraDataCelestials != null && cameraDataDisplay != null)
                {
                    cameraDataCelestials.renderType = CameraRenderType.Base;

                    // The display camera which renders the game, becomes an overlay camera
                    cameraDataDisplay.renderType = CameraRenderType.Overlay;
                    displayCamera.clearFlags = CameraClearFlags.Depth;

                    // Add the main overlay camera to the cameraStack list on the celestials camera
                    cameraDataCelestials.cameraStack.Add(displayCamera);
                }
                #elif SSC_HDRP
                var cameraDataDisplay = displayCamera.GetComponent<HDAdditionalCameraData>();
                if (cameraDataDisplay != null)
                {
                    cameraDataDisplay.clearColorMode = HDAdditionalCameraData.ClearColorMode.None;
                    //cameraDataDisplay.backgroundColorHDR = nightSkyColour;
                    cameraDataDisplay.volumeLayerMask = 0;
                    displayCamera.clearFlags = CameraClearFlags.Depth;
                }

                #if false
                // Visual Enviroment Volume override to select Physically based sky, HDRI sky, Gradient sky, or custom sky fx.

                Debug.Log("[DEBUG] ConfigCameras C");

                HDAdditionalCameraData hadCamDataDisplay;
                if (!displayCamera.TryGetComponent(out hadCamDataDisplay))
                {
                    hadCamDataDisplay = displayCamera.gameObject.AddComponent<HDAdditionalCameraData>();
                }

                if (hadCamDataDisplay != null)
                {
                    Debug.Log("[DEBUG] ConfigCameras D");

                    // Main display camera
                    hadCamDataDisplay.clearColorMode = HDAdditionalCameraData.ClearColorMode.None;
                    hadCamDataDisplay.backgroundColorHDR = nightSkyColour;
                    hadCamDataDisplay.clearDepth = true;
                    //cameraDataDisplay.volumeLayerMask = ~0;
                    //displayCamera.clearFlags = CameraClearFlags.Depth;
                }
                #endif

                #else
                displayCamera.clearFlags = CameraClearFlags.Depth;

                #endif
            }
        }

        protected void DestroyStarGameObject()
        {
            Transform starObj = transform.Find("SSC Stars");
            if (starObj != null) { DestroyImmediate(starObj.gameObject); }
        }

        /// <summary>
        /// Find the celestials camera in the scene or create a new one.
        /// This is a little simplistic and doesn't add the celestials camera
        /// if the parent transform already exists.
        /// </summary>
        /// <param name="cameraName"></param>
        /// <returns></returns>
        protected Transform GetorCreateCamera(string cameraName, out Camera celestialsCamera)
        {
            celestialsCamera = null;

            Transform celestialCameraTrm = transform.Find(cameraName);
            if (celestialCameraTrm == null)
            {
                GameObject celestialsCameraObject = new GameObject(cameraName);

                if (celestialsCameraObject != null)
                {
                    celestialsCameraObject.transform.parent = transform;
                    celestialsCameraObject.transform.localPosition = Vector3.zero;
                    celestialsCamera = celestialsCameraObject.AddComponent<Camera>();
                    celestialCameraTrm = celestialsCameraObject.transform;
                }
            }
            else
            {
                celestialsCamera = celestialCameraTrm.GetComponent<Camera>();
            }

            return celestialCameraTrm;
        }

        /// <summary>
        /// Verify if the star settings look acceptable
        /// </summary>
        /// <returns></returns>
        protected bool IsVerifyStarSettings()
        {
            bool isVerified = false;

            if (starMaterial == null)
            {
                #if UNITY_EDITOR
                Debug.LogWarning("SSC Celestials - the star material is not set");
                #endif
            }
            else if (starMesh == null)
            {
                #if UNITY_EDITOR
                Debug.LogWarning("SSC Celestials - the star mesh is not set");
                #endif
            }
            else if (starSize <= 0f)
            {
                #if UNITY_EDITOR
                Debug.LogWarning("SSC Celestials - the star size must be greater than 0");
                #endif
            }
            else if (numberOfStars < 1)
            {
                #if UNITY_EDITOR
                Debug.LogWarning("SSC Celestials - the number of stars must be greater than 0");
                #endif
            }
            else
            {
                isVerified = true;
            }

            return isVerified;
        }

        /// <summary>
        /// Can be called indirectly from the static method SyncStars().
        /// </summary>
        protected void ManuallyRotateStars()
        {
            #if !SSC_HDRP
            if (isInitialised && timingTypeInt == timingTypeIntManual)
            {
                UpdateCelestialsRotation();
            }
            #endif
        }

        protected void UpdateCelestialsRotation()
        {
            if (camera1 != null)
            {
                celestialsCamera1.transform.rotation = camera1.transform.rotation;
                celestialsCamera1.fieldOfView = camera1.fieldOfView;
            }

            if (isCamera2Initialised)
            {
                celestialsCamera2.transform.rotation = camera2.transform.rotation;
                celestialsCamera2.fieldOfView = camera2.fieldOfView;
            }
        }

        #endregion

        #region Public API Member Methods

        /// <summary>
        /// Attempt to disable the horizon
        /// </summary>
        public void DisableHorizon()
        {
            useHorizon = false;

            #if SSC_HDRP
            if (isInitialised && matHasHorizonId != 0)
            {
                skyMaterial.SetFloat(matHasHorizonId, 0f);
            }
            #endif
        }

        /// <summary>
        /// Attempt to enable the horizon
        /// </summary>
        public void EnableHorizon()
        {
            useHorizon = true;

            #if SSC_HDRP
            if (isInitialised && matHasHorizonId != 0)
            {
                skyMaterial.SetFloat(matHasHorizonId, 1f);
            }
            #endif
        }

        /// <summary>
        /// Get a celestial (planet) by using its unique identifier.
        /// </summary>
        /// <param name="celestialId"></param>
        public SSCCelestial GetCelestialByID(int celestialId)
        {
            SSCCelestial celestial = null;

            if (celestialId != 0 && celestialList != null)
            {
                for (int cIdx = 0; cIdx < celestialList.Count; cIdx++)
                {
                    SSCCelestial _tempCelestial = celestialList[cIdx];
                    if (_tempCelestial != null && _tempCelestial.celestialId == celestialId)
                    {
                        celestial = _tempCelestial;
                        break;
                    }
                }
            }

            return celestial;
        }

        /// <summary>
        /// Get a celestial (planet) by using its zero-based index in the list
        /// </summary>
        /// <param name="index"></param>
        public SSCCelestial GetCelestialByIndex(int index)
        {
            int numCelestials = celestialList == null ? 0 : celestialList.Count;

            if (index >= 0 && index < numCelestials)
            {
                return celestialList[index];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get a celestial (planet) by using its case-sensitive name.
        /// WARNING: This will increase GC, so where possible, instead
        /// use either GetCelestialByIndex or GetCelestialById.
        /// </summary>
        /// <param name="celestialName"></param>
        /// <returns></returns>
        public SSCCelestial GetCelestialByName(string celestialName)
        {
            SSCCelestial celestial = null;

            int numCelestials = celestialList == null ? 0 : celestialList.Count;

            if (numCelestials > 0 && !string.IsNullOrEmpty(celestialName))
            {
                celestial = celestialList.Find(c => c.name == celestialName);
            }

            return celestial;
        }

        /// <summary>
        /// Get the relative distance between the celestial camera and the planet
        /// </summary>
        /// <param name="celestial"></param>
        /// <param name="forceRecalc">If position has been changed outside SSCCelestials, recalculate its distance</param>
        /// <returns></returns>
        public float GetCelestialDistance(SSCCelestial celestial, bool forceRecalc = false)
        {
            if (celestial != null) // && !celestial.isHidden)
            {
                if (!forceRecalc)
                {
                    return celestial.currentCelestialDistance;
                }
                else if (isInitialised && celestialsCamera1 != null)
                {
                    // Planet (A) Camera (B). Direction = (B-A).normalized
                    Vector3 _lookVector = celestialsCamera1.transform.position - celestial.celestialTfrm.position;
                    celestial.currentCelestialDistance = _lookVector.magnitude - minCelestialCameraDistance;
                    return celestial.currentCelestialDistance;
                }
                else if (isCamera2Initialised && celestialsCamera2 != null)
                {
                    // Planet (A) Camera (B). Direction = (B-A).normalized
                    Vector3 _lookVector = celestialsCamera2.transform.position - celestial.celestialTfrm.position;
                    celestial.currentCelestialDistance = _lookVector.magnitude - minCelestialCameraDistance;
                    return celestial.currentCelestialDistance;
                }
                #if SSC_HDRP
                else if (isInitialised && camera1 != null)
                {
                    // Planet (A) Camera (B). Direction = (B-A).normalized
                    Vector3 _lookVector = camera1.transform.position - celestial.celestialTfrm.position;
                    celestial.currentCelestialDistance = _lookVector.magnitude - minCelestialCameraDistance;
                    return celestial.currentCelestialDistance;
                }
                #endif
                else { return 0f; }
            }
            else { return 0f; }
        }

        /// <summary>
        /// Hide the planet baesd on its zero-based position in the list
        /// </summary>
        /// <param name="planetIndex"></param>
        public void HidePlanet (int planetIndex)
        {
            SSCCelestial planet = GetCelestialByIndex(planetIndex);

            if (planet != null && planet.CelestialTransform != null)
            {
                planet.CelestialTransform.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// If the stars have already been created, attempt to hide them.
        /// </summary>
        public void HideStars()
        {
            #if SSC_HDRP
            if (isInitialised && matFadeStarsId != 0)
            {
                skyMaterial.SetFloat(matFadeStarsId, 1f);
            }
            #else
            if (starMRenderer != null)
            {
                starMRenderer.enabled = false;
            }
            #endif
        }

        /// <summary>
        /// Initialise the second camera.
        /// Has no effect if using HDRP.
        /// </summary>
        public void InitialiseCamera2()
        {
            if (isInitialised)
            {
                #if !SSC_HDRP
                // Only add celestials camera if it doesn't already exist
                Transform celestialCamera2Trm = GetorCreateCamera("Celestials Camera 2", out celestialsCamera2);

                if (celestialCamera2Trm != null && celestialsCamera2 != null)
                {
                    ConfigCameras(celestialsCamera2, camera2, -102f);
                    isCamera2Initialised = true;
                }
                #endif
            }
        }

        /// <summary>
        /// Call this if you have changed any of the settings on the camera1 and/or camera2.
        /// Has no effect if using HDRP.
        /// </summary>
        public void RefreshCameras()
        {
            if (IsInitialised)
            {
                #if !SSC_HDRP
                ConfigCameras(celestialsCamera1, camera1, -100f);
                ConfigCameras(celestialsCamera2, camera2, -102f);
                #endif
            }
        }

        /// <summary>
        /// Call this when changing the number of stars and/or planets.
        /// </summary>
        public void RefreshCelestials()
        {
            if (isInitialised && IsVerifyStarSettings())
            {
                BuildCelestials();
            }
        }

        /// <summary>
        /// Set the relative distance the celestial object (planet) is from the celestials camera(s).
        /// </summary>
        /// <param name="celestial"></param>
        /// <param name="relativeDistance">Value must be between 0.0 and 1.0</param>
        public void SetCelestialDistance (SSCCelestial celestial, float relativeDistance)
        {
            if (relativeDistance >= 0f && relativeDistance <= 1f && celestial != null && !celestial.isHidden)
            {
                celestial.currentCelestialDistance = relativeDistance;

                Vector3 celestialPos = CalcCelestialPosition(celestial);

                celestial.celestialTfrm.position = celestialPos;
            }
        }

        /// <summary>
        /// Attempt to set the fade stars value in HDRP.
        /// Has no effect in BiRP or URP.
        /// </summary>
        /// <param name="newValue"></param>
        public void SetFadeStars (float newValue)
        {
            #if SSC_HDRP
            if (isInitialised && matFadeStarsId != 0)
            {
                skyMaterial.SetFloat(matFadeStarsId, newValue);
            }
            #endif
        }

        /// <summary>
        /// Attempt to set the star colour range from in HDRP.
        /// Has no effect in BiRP or URP.
        /// </summary>
        /// <param name="newValue"></param>
        public void SetStarColourFrom (Color newColour)
        {
            #if SSC_HDRP
            if (isInitialised && matColourFromId != 0)
            {
                skyMaterial.SetColor(matColourFromId, newColour);
            }
            #endif
        }

        /// <summary>
        /// Attempt to set the star colour range to in HDRP.
        /// Has no effect in BiRP or URP.
        /// </summary>
        /// <param name="newValue"></param>
        public void SetStarColourTo (Color newColour)
        {
            #if SSC_HDRP
            if (isInitialised && matColourToId != 0)
            {
                skyMaterial.SetColor(matColourToId, newColour);
            }
            #endif
        }

        /// <summary>
        /// Attempt to set the star density in HDRP.
        /// Has no effect in BiRP or URP.
        /// </summary>
        /// <param name="newValue"></param>
        public void SetStarDensity (float newValue)
        {
            #if SSC_HDRP
            if (isInitialised && matDensityId != 0)
            {
                skyMaterial.SetFloat(matDensityId, newValue);
            }
            #endif
        }

        /// <summary>
        /// Attempt to set the starfield in HDRP.
        /// Has no effect in BiRP or URP.
        /// </summary>
        /// <param name="newValue"></param>
        public void SetStarfield (float newValue)
        {
            #if SSC_HDRP
            if (isInitialised && matStarfieldId != 0)
            {
                skyMaterial.SetFloat(matStarfieldId, newValue);
            }
            #endif
        }

        /// <summary>
        /// Attempt to set the horizon in HDRP.
        /// Has no effect in BiRP or URP.
        /// </summary>
        /// <param name="newValue"></param>
        public void SetHorizon (float newValue)
        {
            #if SSC_HDRP
            if (isInitialised && matHorizonId != 0)
            {
                skyMaterial.SetFloat(matHorizonId, newValue);
            }
            #endif
        }

        /// <summary>
        /// Set the number of stars. After calling this, call RefreshCelestials()
        /// for it to take effect.
        /// </summary>
        /// <param name="newNumberOfStars"></param>
        public void SetNumberOfStars (int newNumberOfStars)
        {
            if (numberOfStars > 0)
            {
                numberOfStars = newNumberOfStars;
            }
        }

        /// <summary>
        /// Change the timing type.
        /// </summary>
        /// <param name="newTimingType"></param>
        public void SetTimingType (TimingType newTimingType)
        {
            timingType = newTimingType;
            timingTypeInt = (int)timingType;
        }

        /// <summary>
        /// Show the planet baesd on its zero-based position in the list
        /// </summary>
        /// <param name="planetIndex"></param>
        public void ShowPlanet (int planetIndex)
        {
            SSCCelestial planet = GetCelestialByIndex(planetIndex);

            if (planet != null && planet.CelestialTransform != null)
            {
                planet.CelestialTransform.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Show the planet
        /// </summary>
        /// <param name="planet"></param>
        public void ShowPlanet (SSCCelestial planet)
        {
            if (planet != null && planet.CelestialTransform != null && !planet.CelestialTransform.gameObject.activeSelf)
            {
                planet.CelestialTransform.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// If the stars have already been created, but then hidden,
        /// attempt to show them.
        /// </summary>
        public void ShowStars()
        {
            #if SSC_HDRP
            if (isInitialised && matFadeStarsId != 0)
            {
                skyMaterial.SetFloat(matFadeStarsId, 0f);
            }
            #else
            if (starMRenderer != null)
            {
                starMRenderer.enabled = true;
            }
            #endif
        }

        /// <summary>
        /// Update the render settings for the scene
        /// </summary>
        public void UpdateRenderSettings()
        {
            // In the Unity Lighting editor for SRP, AmbientMode.Flat = Color and Trilight = Gradient
            RenderSettings.ambientMode = envAmbientSource == EnvAmbientSource.Gradient ? UnityEngine.Rendering.AmbientMode.Trilight : envAmbientSource == EnvAmbientSource.Colour ? UnityEngine.Rendering.AmbientMode.Flat : UnityEngine.Rendering.AmbientMode.Skybox;

            if (overrideAmbientColour)
            {
                RenderSettings.ambientSkyColor = ambientSkyColour;
            }
            else
            {
                RenderSettings.ambientSkyColor = nightSkyColour;
            }
        }

        #endregion

        #region Public Static APIs

        /// <summary>
        /// Attempt to get the celestials script in the scene, given a scene handle.
        /// </summary>
        /// <param name="sceneHandle"></param>
        /// <returns></returns>
        public static Celestials GetStars (int sceneHandle)
        {
            return CelestialsInstance != null && CelestialsInstance.gameObject.scene.handle == sceneHandle ? CelestialsInstance : null;
        }

        /// <summary>
        /// This can be called to manually synchronise the rotation
        /// of the stars to match the camera rotation.
        /// TimingType must be set to Manual.
        /// </summary>
        public static void SyncStars()
        {
            if (CelestialsInstance != null)
            {
                CelestialsInstance.ManuallyRotateStars();
            }
        }

        #endregion
    }
}