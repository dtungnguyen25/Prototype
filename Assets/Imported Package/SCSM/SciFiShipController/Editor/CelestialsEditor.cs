using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    [CustomEditor(typeof(Celestials))]
    public class CelestialsEditor : Editor
    {
        #region GUIContent - Headers
        protected readonly static GUIContent headerContent = new GUIContent("Render stars in the sky. Add distant planets (BiRP or URP).");
        #endregion

        #region GUIContent - General
        protected readonly static GUIContent initialiseOnAwakeContent = new GUIContent(" Initialise on Awake", "If enabled, Initialise() will be called as soon as Awake() runs. This should be disabled if you want to control when the component is enabled through code.");
        protected readonly static GUIContent camera1Content = new GUIContent(" Camera 1", "The main display camera");
        protected readonly static GUIContent useHorizonContent = new GUIContent(" Horizon", "The stars finish at the horizon rather than filling the whole screen.");
        protected readonly static GUIContent isCreateHiddenStarsContent = new GUIContent(" Create Hidden Stars", "Create the stars but hide them immediately");
        protected readonly static GUIContent isCreateHiddenPlanetsContent = new GUIContent(" Create Hidden Planets", "Create the planets that are marked as hidden");
        protected readonly static GUIContent celestialListContent = new GUIContent(" Celestials List", "List of planet or celestial objects");
        #endregion

        #region GUIContent - HDRP
        #if SSC_HDRP
        protected readonly static GUIContent skyVolumeContent = new GUIContent(" Sky Volume", "The HDRP volume used in the scene to configure the HDRi sky");
        protected readonly static GUIContent horizonContent = new GUIContent(" Horizon Position", "The normalised position of the horizon on the y-axis. 0 is the bottom of the screen. 1 is the top (no stars will be rendered).");
        protected readonly static GUIContent densityContent = new GUIContent(" Density", "The density of the stars");
        protected readonly static GUIContent starfieldContent = new GUIContent(" Starfield", "The starfield to be displayed");
        protected readonly static GUIContent fadeStarsContent = new GUIContent(" Fade", "The amount of fade to apply to the stars");
        protected readonly static GUIContent colourFromContent = new GUIContent(" Colour From", "The stars colour range from");
        protected readonly static GUIContent colourToContent = new GUIContent(" Colour To", "The stars colour range to");
        #endif
        #endregion

        #region Custom Editor protected variables
        // These are visible to inherited classes

        protected Celestials celestials = null;

        // Formatting and style variables
        protected string labelText;
        protected GUIStyle labelFieldRichText;
        protected GUIStyle helpBoxRichText;
        protected GUIStyle buttonCompact;
        protected float defaultEditorLabelWidth = 0f;
        protected float defaultEditorFieldWidth = 0f;
        protected bool isDebuggingEnabled = false;
        protected bool isStylesInitialised = false;
        protected bool isSceneModified = false;
        protected GUIStyle headingFieldRichText;
        protected GUIStyle foldoutStyleNoLabel;
        protected GUIStyle helpLabelRichText;
        protected GUIStyle toggleCompactButtonStyleNormal = null;  // Small Toggle button. e.g. G(izmo) on/off
        protected GUIStyle toggleCompactButtonStyleToggled = null;
        protected Color separatorColor = new Color();

        #endregion

        #region Serialized Properties - General
        protected SerializedProperty initialiseOnAwakeProp;
        protected SerializedProperty camera1Prop;
        protected SerializedProperty useHorizonProp;
        protected SerializedProperty isCreateHiddenStarsProp;
        protected SerializedProperty isCreateHiddenPlanetsProp;
        protected SerializedProperty celestialListProp;
        #endregion

        #region Serialized Properties - HDRP
        #if SSC_HDRP
        protected SerializedProperty skyVolumeProp;
        protected SerializedProperty horizonProp;
        protected SerializedProperty densityProp;
        protected SerializedProperty starfieldProp;
        protected SerializedProperty fadeStarsProp;
        protected SerializedProperty colourFromProp;
        protected SerializedProperty colourToProp;
        #endif
        #endregion

        #region Events

        public virtual void OnEnable()
        {
            celestials = (Celestials)target;

            defaultEditorLabelWidth = 150f; // EditorGUIUtility.labelWidth;
            defaultEditorFieldWidth = EditorGUIUtility.fieldWidth;

            separatorColor = EditorGUIUtility.isProSkin ? new Color(0.2f, 0.2f, 0.2f, 2f) : Color.grey;

            // Reset GUIStyles
            isStylesInitialised = false;
            toggleCompactButtonStyleNormal = null;
            toggleCompactButtonStyleToggled = null;
            foldoutStyleNoLabel = null;

            #region Find Properties - General
            initialiseOnAwakeProp = serializedObject.FindProperty("initialiseOnAwake");
            camera1Prop = serializedObject.FindProperty("camera1");
            useHorizonProp = serializedObject.FindProperty("useHorizon");
            isCreateHiddenStarsProp = serializedObject.FindProperty("isCreateHiddenStars");
            isCreateHiddenPlanetsProp = serializedObject.FindProperty("isCreateHiddenPlanets");
            celestialListProp = serializedObject.FindProperty("celestialList");

            #endregion

            #region Find Properties - HDRP
            #if SSC_HDRP
            skyVolumeProp = serializedObject.FindProperty("skyVolume");
            horizonProp = serializedObject.FindProperty("horizon");
            densityProp = serializedObject.FindProperty("density");
            starfieldProp = serializedObject.FindProperty("starfield");
            fadeStarsProp = serializedObject.FindProperty("fadeStars");
            colourFromProp = serializedObject.FindProperty("colourFrom");
            colourToProp = serializedObject.FindProperty("colourTo");
            #endif
            #endregion
        }

        #endregion

        #region Protected Non-virtual Methods

        /// <summary>
        /// Set up the buttons and styles used in OnInspectorGUI.
        /// Call this near the top of OnInspectorGUI.
        /// </summary>
        protected void ConfigureButtonsAndStyles()
        {
            // Set up rich text GUIStyles
            if (!isStylesInitialised)
            {
                helpBoxRichText = new GUIStyle("HelpBox");
                helpBoxRichText.richText = true;

                labelFieldRichText = new GUIStyle("Label");
                labelFieldRichText.richText = true;

                headingFieldRichText = new GUIStyle(UnityEditor.EditorStyles.miniLabel);
                headingFieldRichText.richText = true;
                headingFieldRichText.normal.textColor = helpBoxRichText.normal.textColor;

                // Overide default styles
                EditorStyles.foldout.fontStyle = FontStyle.Bold;

                // When using a no-label foldout, don't forget to set the global
                // EditorGUIUtility.fieldWidth to a small value like 15, then back
                // to the original afterward.
                foldoutStyleNoLabel = new GUIStyle(EditorStyles.foldout);
                foldoutStyleNoLabel.fixedWidth = 0.01f;

                helpLabelRichText = new GUIStyle("Label");
                helpLabelRichText.richText = true;
                helpLabelRichText.wordWrap = true;
                helpLabelRichText.font = helpBoxRichText.font;
                helpLabelRichText.fontSize = helpBoxRichText.fontSize;
                helpLabelRichText.fontStyle = helpBoxRichText.fontStyle;
                helpLabelRichText.normal = helpBoxRichText.normal;

                buttonCompact = new GUIStyle("Button");
                buttonCompact.fontSize = 10;

                // Create a new button or else will effect the Button style for other buttons too
                toggleCompactButtonStyleNormal = new GUIStyle("Button");
                toggleCompactButtonStyleToggled = new GUIStyle(toggleCompactButtonStyleNormal);
                toggleCompactButtonStyleNormal.fontStyle = FontStyle.Normal;
                toggleCompactButtonStyleToggled.fontStyle = FontStyle.Bold;
                toggleCompactButtonStyleToggled.normal.background = toggleCompactButtonStyleToggled.active.background;

                isStylesInitialised = true;
            }
        }

        #endregion

        #region Protected Virtual Methods

        /// <summary>
        /// This function overides what is normally seen in the inspector window
        /// This allows stuff like buttons to be drawn there.
        /// </summary>
        protected virtual void DrawBaseInspector()
        {
            ConfigureButtonsAndStyles();

            #region Headers and Info Buttons
            GUILayout.BeginVertical("HelpBox");
            SSCEditorHelper.SSCVersionHeader(labelFieldRichText);
            GUILayout.EndVertical();
            EditorGUILayout.LabelField(headerContent, helpBoxRichText);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            SSCEditorHelper.DrawSSCGetHelpButtons(buttonCompact);
            EditorGUILayout.EndVertical();
            #endregion

            serializedObject.Update();

            EditorGUILayout.BeginVertical("HelpBox");
            DrawGeneralSettings();
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draw the general settings in the inspector
        /// </summary>
        protected virtual void DrawGeneralSettings()
        {
        
            #if SSC_HDRP

            #if !UNITY_2021_3_OR_NEWER
            EditorGUILayout.HelpBox("Celestials for HDRP requires U2021.3+ and HDRP 12.x+", MessageType.Error);
            #endif

            SSCEditorHelper.InTechPreview(true);
            EditorGUILayout.PropertyField(initialiseOnAwakeProp, initialiseOnAwakeContent);
            EditorGUILayout.PropertyField(skyVolumeProp, skyVolumeContent);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(camera1Prop, camera1Content);
            if (EditorGUI.EndChangeCheck())
            {
                //serializedObject.ApplyModifiedProperties();
                //celestials.SetCamera1();
                //serializedObject.Update();
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(useHorizonProp, useHorizonContent);
            if (EditorGUI.EndChangeCheck())
            {
                if (useHorizonProp.boolValue) { celestials.EnableHorizon(); }
                else { celestials.DisableHorizon(); }
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(horizonProp, horizonContent);
            if (EditorGUI.EndChangeCheck())
            {
                celestials.SetHorizon(horizonProp.floatValue);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(starfieldProp, starfieldContent);
            if (EditorGUI.EndChangeCheck())
            {
                celestials.SetStarfield(starfieldProp.floatValue);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(densityProp, densityContent);
            if (EditorGUI.EndChangeCheck())
            {
                celestials.SetStarDensity(densityProp.floatValue);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(fadeStarsProp, fadeStarsContent);
            if (EditorGUI.EndChangeCheck())
            {
                celestials.SetFadeStars(fadeStarsProp.floatValue);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(colourFromProp, colourFromContent);
            if (EditorGUI.EndChangeCheck())
            {
                celestials.SetStarColourFrom(colourFromProp.colorValue);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(colourToProp, colourToContent);
            if (EditorGUI.EndChangeCheck())
            {
                celestials.SetStarColourFrom(colourToProp.colorValue);
            }

            EditorGUILayout.PropertyField(isCreateHiddenStarsProp, isCreateHiddenStarsContent);
            EditorGUILayout.PropertyField(isCreateHiddenPlanetsProp, isCreateHiddenPlanetsContent);
            EditorGUILayout.PropertyField(celestialListProp, celestialListContent);

            #else
            DrawDefaultInspector();
            #endif
        }

        #endregion

        #region OnInspectorGUI

        public override void OnInspectorGUI()
        {
            #if !SCSM_SSC
            EditorGUILayout.HelpBox("Sci-Fi Ship Controller asset is missing from the project.", MessageType.Error);
            #else
            DrawBaseInspector();
            #endif
        }

        #endregion
    }
}