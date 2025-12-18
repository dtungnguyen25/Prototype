using UnityEditor;
using System.Collections.Generic;
using UnityEngine;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    [CustomEditor(typeof(ShipCameraChanger))]
    public class ShipCameraChangerEditor : Editor
    {
        #region Custom Editor protected variables
        // These are visible to inherited classes
        protected ShipCameraChanger shipCameraChanger;
        protected bool isStylesInitialised = false;
        protected bool isSceneModified = false;
        protected string labelText;
        protected GUIStyle labelFieldRichText;
        protected GUIStyle headingFieldRichText;
        protected GUIStyle helpBoxRichText;
        protected GUIStyle buttonCompact;
        protected GUIStyle foldoutStyleNoLabel;
        protected GUIStyle toggleCompactButtonStyleNormal = null;  // Small Toggle button. e.g. G(izmo) on/off
        protected GUIStyle toggleCompactButtonStyleToggled = null;
        protected Color separatorColor = new Color();
        protected float defaultEditorLabelWidth = 0f;
        protected float defaultEditorFieldWidth = 0f;
        protected bool isDebuggingEnabled = false;

        protected int cameraSettingsDeletePos = -1;
        #endregion

        #region GUIContent - Headers
        private readonly static GUIContent headerContent = new GUIContent("This component, which should be attached to a ShipControlModule (aka ship), can change the ShipCameraModule settings at runtime.");
        private readonly static GUIContent[] tabTexts = { new GUIContent("General"), new GUIContent("Events") };
        #endregion

        #region GUIContent - General
        protected readonly static GUIContent initialiseOnStartContent = new GUIContent(" Initialise on Start", "If enabled, Initialise() will be called as soon as Start() runs. This should be disabled if you want to control when the component is initialised through code.");
        protected readonly static GUIContent isEnableOnInitContent = new GUIContent(" Enable on Init", "Should the component be ready for use when it is first initialised?");
        protected readonly static GUIContent cameraSettingOnInitContent = new GUIContent(" Cam Setting on Init", "The camera settings to apply when the component is initialised. If the value is 0 or invalid, none will be applied.");
        protected readonly static GUIContent cameraSettingOnFirstContent = new GUIContent(" Cam Setting on First", "If the settings are not applied when first initialised, which settings should be applied when cycled the first time?");
        protected readonly static GUIContent shipCameraModuleContent = new GUIContent(" Ship Camera Module", "The module used to control the player ship camera");
        protected readonly static GUIContent isAlwaysSnapToTargetContent = new GUIContent(" Always Snap to Target", "When applying new settings, always snap the camera to the target location and rotation. This will override the SnapToTarget in the ShipCameraSettings scriptable object.");
        protected readonly static GUIContent cameraSettingsContent = new GUIContent(" Camera Settings");
        #endregion

        #region GUIContent - Events
        protected readonly static GUIContent onChangeCameraSettingsContent = new GUIContent("On Change Camera Settings");
        #endregion

        #region GUIContent - Debug
        private readonly static GUIContent debugModeContent = new GUIContent(" Debug Mode", "Use this to display the data about the ShipWarpModule component at runtime in the editor.");
        private readonly static GUIContent debugIsInitialisedContent = new GUIContent(" Is Initialised?");
        private readonly static GUIContent debugIsChangerEnabledContent = new GUIContent(" Is Enabled?");
        private readonly static GUIContent debugCurrentCameraSettingsIndexContent = new GUIContent(" Camera Settings");
        #endregion

        #region Serialized Properties - General
        protected SerializedProperty selectedTabIntProp;
        protected SerializedProperty initialiseOnStartProp;
        protected SerializedProperty isEnableOnInitProp;
        protected SerializedProperty cameraSettingOnInitProp;
        protected SerializedProperty cameraSettingOnFirstProp;
        protected SerializedProperty shipCameraModuleProp;
        protected SerializedProperty isAlwaysSnapToTargetProp;
        protected SerializedProperty shipCameraSettingsListProp;
        protected SerializedProperty shipCameraSettingsProp;
        #endregion

        #region Serialized Properties - Events
        protected SerializedProperty onChangeCameraSettingsProp;
        #endregion

        #region Events

        protected virtual void OnEnable()
        {
            shipCameraChanger = (ShipCameraChanger)target;

            defaultEditorLabelWidth = 185f;
            defaultEditorFieldWidth = EditorGUIUtility.fieldWidth;

            separatorColor = EditorGUIUtility.isProSkin ? new Color(0.2f, 0.2f, 0.2f, 2f) : Color.grey;

            // Reset GUIStyles
            isStylesInitialised = false;
            toggleCompactButtonStyleNormal = null;
            toggleCompactButtonStyleToggled = null;
            foldoutStyleNoLabel = null;

            #region Find Properties - General
            selectedTabIntProp = serializedObject.FindProperty("selectedTabInt");
            initialiseOnStartProp = serializedObject.FindProperty("initialiseOnStart");
            isEnableOnInitProp = serializedObject.FindProperty("isEnableOnInit");
            cameraSettingOnInitProp = serializedObject.FindProperty("cameraSettingOnInit");
            cameraSettingOnFirstProp = serializedObject.FindProperty("cameraSettingOnFirst");
            shipCameraModuleProp = serializedObject.FindProperty("shipCameraModule");
            isAlwaysSnapToTargetProp = serializedObject.FindProperty("isAlwaysSnapToTarget");
            shipCameraSettingsListProp = serializedObject.FindProperty("shipCameraSettingsList");
            #endregion

            #region Find Properties - Events
            onChangeCameraSettingsProp = serializedObject.FindProperty("onChangeCameraSettings");
            #endregion

        }
        #endregion

        #region Private and Protected Methods

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

        /// <summary>
        /// Draw enable or disable debugging in the inspector
        /// </summary>
        protected void DrawDebugToggle()
        {
            isDebuggingEnabled = EditorGUILayout.Toggle(SSCEditorHelper.debugModeIndent1Content, isDebuggingEnabled);
        }

        #endregion

        #region Protected Virtual Methods

        /// <summary>
        /// This function overides what is normally seen in the inspector window
        /// This allows stuff like buttons to be drawn there
        /// </summary>
        protected virtual void DrawBaseInspector()
        {
            #region Initialise
            shipCameraChanger.allowRepaint = false;
            EditorGUIUtility.labelWidth = defaultEditorLabelWidth;
            EditorGUIUtility.fieldWidth = defaultEditorFieldWidth;
            isSceneModified = false;
            #endregion

            ConfigureButtonsAndStyles();

            // Read in all the properties
            serializedObject.Update();

            #region Headers and Info Buttons
            SSCEditorHelper.SSCVersionHeader(labelFieldRichText);
            EditorGUILayout.LabelField(headerContent, helpBoxRichText);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            SSCEditorHelper.DrawSSCGetHelpButtons(buttonCompact);
            DrawToolBar(tabTexts);
            EditorGUILayout.EndVertical();
            #endregion

            EditorGUILayout.BeginVertical("HelpBox");

            #region General Settings
            if (selectedTabIntProp.intValue == 0)
            {
                EditorGUILayout.PropertyField(initialiseOnStartProp, initialiseOnStartContent);
                EditorGUILayout.PropertyField(isEnableOnInitProp, isEnableOnInitContent);
                EditorGUILayout.PropertyField(cameraSettingOnInitProp, cameraSettingOnInitContent);
                if (cameraSettingOnInitProp.intValue == 0)
                {
                    EditorGUILayout.PropertyField(cameraSettingOnFirstProp, cameraSettingOnFirstContent);
                }
                EditorGUILayout.PropertyField(shipCameraModuleProp, shipCameraModuleContent);
                EditorGUILayout.PropertyField(isAlwaysSnapToTargetProp, isAlwaysSnapToTargetContent);

                DrawCameraSettings();
            }
            #endregion

            #region Event Settings
            else if (selectedTabIntProp.intValue == 1)
            {
                DrawEventSettings();
            }
            #endregion

            EditorGUILayout.EndVertical();

            // Apply property changes
            serializedObject.ApplyModifiedProperties();

            shipCameraChanger.allowRepaint = true;

            DrawDebugSettings();
        }

        /// <summary>
        /// Draw the event settings in the inspector
        /// </summary>
        protected virtual void DrawEventSettings()
        {
            EditorGUILayout.PropertyField(onChangeCameraSettingsProp, onChangeCameraSettingsContent);
        }

        /// <summary>
        /// Draw the settings for the camera in the inspector
        /// </summary>
        protected virtual void DrawCameraSettings()
        {
            cameraSettingsDeletePos = -1;
            int numCamSettings = shipCameraSettingsListProp.arraySize;

            #region Add or Remove Camera Settings
            SSCEditorHelper.DrawSSCHorizontalGap(4f);

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(cameraSettingsContent);

            if (GUILayout.Button("+", GUILayout.MaxWidth(30f)))
            {
                shipCameraSettingsListProp.arraySize++;
                numCamSettings = shipCameraSettingsListProp.arraySize;
            }
            if (GUILayout.Button("-", GUILayout.MaxWidth(30f)))
            {
                if (numCamSettings > 0) { cameraSettingsDeletePos = shipCameraSettingsListProp.arraySize - 1; }
            }
            GUILayout.EndHorizontal();

            #endregion

            #region Camera Settings List

            for (int csIdx = 0; csIdx < numCamSettings; csIdx++)
            {
                shipCameraSettingsProp = shipCameraSettingsListProp.GetArrayElementAtIndex(csIdx);

                if (shipCameraSettingsProp != null)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(" " + (csIdx + 1).ToString("00") + ".", GUILayout.Width(25f));

                    EditorGUILayout.PropertyField(shipCameraSettingsProp, GUIContent.none);

                    if (GUILayout.Button("X", buttonCompact, GUILayout.MaxWidth(20f))) { cameraSettingsDeletePos = csIdx; }
                    GUILayout.EndHorizontal();
                }
            }

            #endregion

            #region Delete Camera Settings
            if (cameraSettingsDeletePos >= 0)
            {
                shipCameraSettingsListProp.DeleteArrayElementAtIndex(cameraSettingsDeletePos);
                cameraSettingsDeletePos = -1;

                serializedObject.ApplyModifiedProperties();
                // In U2019.4+ avoid: EndLayoutGroup: BeginLayoutGroup must be called first.
                GUIUtility.ExitGUI();
            }
            #endregion
        }

        /// <summary>
        /// Draw the debug settings in the inspector
        /// </summary>
        protected virtual void DrawDebugSettings()
        {
            // NOTE: This is NOT performance optimised - can create GC issues and other performance overhead.
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            isDebuggingEnabled = EditorGUILayout.Toggle(debugModeContent, isDebuggingEnabled);
            if (isDebuggingEnabled && shipCameraChanger != null)
            {
                #region Debugging - General

                SSCEditorHelper.PerformanceImpact();

                float rightLabelWidth = 150f;
                bool isChangerInitialised = shipCameraChanger.IsInitialised;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(debugIsInitialisedContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
                EditorGUILayout.LabelField(isChangerInitialised ? "Yes" : "No", GUILayout.MaxWidth(rightLabelWidth));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(debugIsChangerEnabledContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
                EditorGUILayout.LabelField(shipCameraChanger.IsChangerEnabled ? "Yes" : "No", GUILayout.MaxWidth(rightLabelWidth));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(debugCurrentCameraSettingsIndexContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
                EditorGUILayout.LabelField(shipCameraChanger.CurrentCameraSettingsIndex < 0 ? "Not Set" : (shipCameraChanger.CurrentCameraSettingsIndex + 1).ToString("00"), GUILayout.MaxWidth(rightLabelWidth));
                EditorGUILayout.EndHorizontal();

                #endregion
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draw the toolbar using the supplied array of tab text.
        /// </summary>
        /// <param name="tabGUIContent"></param>
        protected virtual void DrawToolBar(GUIContent[] tabGUIContent)
        {
            int prevTab = selectedTabIntProp.intValue;

            // Show a toolbar to allow the user to switch between viewing different areas
            selectedTabIntProp.intValue = GUILayout.Toolbar(selectedTabIntProp.intValue, tabGUIContent);

            // When switching tabs, disable focus on previous control
            if (prevTab != selectedTabIntProp.intValue) { GUI.FocusControl(null); }
        }

        #endregion

        #region OnInspectorGUI

        public override void OnInspectorGUI()
        {
            DrawBaseInspector();
        }

        #endregion
    }
}