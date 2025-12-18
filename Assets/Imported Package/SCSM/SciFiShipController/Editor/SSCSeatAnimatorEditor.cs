using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    [CustomEditor(typeof(SSCSeatAnimator))]
    public class SSCSeatAnimatorEditor : Editor
    {
        #region Custom Editor protected variables
        // These are visible to inherited classes
        protected SSCSeatAnimator seatAnimator;
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
        protected int numStages = 0;
        #endregion

        #region GUIContent - Headers
        private readonly static GUIContent headerContent = new GUIContent("This component enables you to perform a multi-stage animation to rotate and move a seat. Typically used for cockpit seating. You may need to perform external operations during the stages using the events.");

        #endregion

        #region GUIContent - General
        protected readonly static GUIContent initialiseOnAwakeContent = new GUIContent(" Initialise on Awake", "If enabled, Initialise() will be called as soon as Awake() runs. This should be disabled if you want to control when the component is enabled through code.");
        protected readonly static GUIContent activateContent = new GUIContent(" Activate");
        protected readonly static GUIContent activateStartStageNameContent = new GUIContent("  Start Stage Name", "The name of the stage that is played first when Activate() is called.");
        protected readonly static GUIContent activateEndStageNameContent = new GUIContent("  End Stage Name", "The name of the stage that, when completed, will mark the seat as activated.");
        protected readonly static GUIContent deactivateContent = new GUIContent(" Deactivate");
        protected readonly static GUIContent deactivateStartStageNameContent = new GUIContent("  Start Stage Name", "The name of the stage that is played first when Deactivate() is called.");
        protected readonly static GUIContent deactivateEndStageNameContent = new GUIContent("  End Stage Name", "The name of the stage that, when completed, will mark the seat as deactivated.");
        protected readonly static GUIContent seatStageListContent = new GUIContent(" Seat Stages", "");
        #endregion

        #region GUIContent - Debug
        protected readonly static GUIContent debugModeContent = new GUIContent(" Debug Mode", "Use this to display the data about the SeatAnimator component at runtime in the editor.");
        //private readonly static GUIContent debugNotSetContent = new GUIContent("-", "not set");
        protected readonly static GUIContent debugIsInitialisedContent = new GUIContent(" Is Initialised?");
        protected readonly static GUIContent debugIsActivatedContent = new GUIContent(" Is Activated?");
        protected readonly static GUIContent debugIsActivatingContent = new GUIContent(" Is Activating?");
        protected readonly static GUIContent debugIsDeactivatingContent = new GUIContent(" Is Deactivating?");
        #endregion

        #region Serialized Properties - General
        protected SerializedProperty initialiseOnAwakeProp;
        protected SerializedProperty activateStartStageNameProp;
        protected SerializedProperty activateEndStageNameProp;
        protected SerializedProperty deactivateStartStageNameProp;
        protected SerializedProperty deactivateEndStageNameProp;
        protected SerializedProperty audioClipProp;
        protected SerializedProperty seatStageListProp;
        #endregion

        #region Events

        protected virtual void OnEnable()
        {
            seatAnimator = (SSCSeatAnimator)target;

            defaultEditorLabelWidth = 185f;
            defaultEditorFieldWidth = EditorGUIUtility.fieldWidth;

            separatorColor = EditorGUIUtility.isProSkin ? new Color(0.2f, 0.2f, 0.2f, 2f) : Color.grey;

            // Reset GUIStyles
            isStylesInitialised = false;
            toggleCompactButtonStyleNormal = null;
            toggleCompactButtonStyleToggled = null;
            foldoutStyleNoLabel = null;

            #region Find Properties - General
            initialiseOnAwakeProp = serializedObject.FindProperty("initialiseOnAwake");
            activateStartStageNameProp = serializedObject.FindProperty("activateStartStageName");
            activateEndStageNameProp = serializedObject.FindProperty("activateEndStageName");
            deactivateStartStageNameProp = serializedObject.FindProperty("deactivateStartStageName");
            deactivateEndStageNameProp = serializedObject.FindProperty("deactivateEndStageName");
            seatStageListProp = serializedObject.FindProperty("seatStageList");
            #endregion
        }

        /// <summary>
        /// Gets called automatically 10 times per second
        /// Comment out if not required
        /// </summary>
        private void OnInspectorUpdate()
        {
            // OnInspectorGUI() only registers events when the mouse is positioned over the custom editor window
            // This code forces OnInspectorGUI() to run every frame, so it registers events even when the mouse
            // is positioned over the scene view
            if (seatAnimator.allowRepaint) { Repaint(); }
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
        /// Draw the debug settings in the inspector
        /// </summary>
        protected virtual void DrawDebugSettings()
        {
            // NOTE: This is NOT performance optimised - can create GC issues and other performance overhead.
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            isDebuggingEnabled = EditorGUILayout.Toggle(debugModeContent, isDebuggingEnabled);
            if (isDebuggingEnabled && seatAnimator != null)
            {
                #region Debugging - General

                SSCEditorHelper.PerformanceImpact();

                float rightLabelWidth = 150f;
                bool isSeatInitialised = seatAnimator.IsInitialised;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(debugIsInitialisedContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
                EditorGUILayout.LabelField(isSeatInitialised ? "Yes" : "No", GUILayout.MaxWidth(rightLabelWidth));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(debugIsActivatedContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
                EditorGUILayout.LabelField(seatAnimator.IsActivated ? "Yes" : "No", GUILayout.MaxWidth(rightLabelWidth));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(debugIsActivatingContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
                EditorGUILayout.LabelField(seatAnimator.IsActivating ? "Yes" : "No", GUILayout.MaxWidth(rightLabelWidth));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(debugIsDeactivatingContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
                EditorGUILayout.LabelField(seatAnimator.IsDeactivating ? "Yes" : "No", GUILayout.MaxWidth(rightLabelWidth));
                EditorGUILayout.EndHorizontal();

                #endregion
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draw the general setting in the inspector
        /// </summary>
        protected void DrawGeneralSettings()
        {
            EditorGUILayout.PropertyField(initialiseOnAwakeProp, initialiseOnAwakeContent);

            EditorGUILayout.LabelField(activateContent);
            SSCEditorHelper.DrawSSCPropertyIndent(10f, activateStartStageNameProp, activateStartStageNameContent, defaultEditorLabelWidth);
            SSCEditorHelper.DrawSSCPropertyIndent(10f, activateEndStageNameProp, activateEndStageNameContent, defaultEditorLabelWidth);

            EditorGUILayout.LabelField(deactivateContent);
            SSCEditorHelper.DrawSSCPropertyIndent(10f, deactivateStartStageNameProp, deactivateStartStageNameContent, defaultEditorLabelWidth);
            SSCEditorHelper.DrawSSCPropertyIndent(10f, deactivateEndStageNameProp, deactivateEndStageNameContent, defaultEditorLabelWidth);

            // Was the number of stages increased while NOT using the + button?
            // e.g. via right-click Duplicate Array Element
            if (numStages > 1 && numStages < seatStageListProp.arraySize)
            {
                numStages = seatStageListProp.arraySize;

                // It is likely a stage was duplicated so need to make sure guidHashes are unqiue
                List<int> hashList = new List<int>(numStages);
                for (int sgIdx = 0; sgIdx < numStages; sgIdx++)
                {
                    int guidHash = seatAnimator.SeatStageList[sgIdx].guidHash;
                    if (hashList.Exists(h => h == guidHash))
                    {
                        // If it already exists, get a new guidHash for this stage.
                        serializedObject.ApplyModifiedProperties();
                        seatAnimator.SeatStageList[sgIdx].guidHash = SSCMath.GetHashCodeFromGuid();
                        serializedObject.Update();
                    }
                    else
                    {
                        hashList.Add(guidHash);
                    }
                }
            }

            numStages = seatStageListProp.arraySize;

            EditorGUILayout.PropertyField(seatStageListProp, seatStageListContent);

            // The list drawer or custom PropertyDrawer doesn't call the class constructor, so do it here.
            if (numStages < seatStageListProp.arraySize)
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(seatAnimator, "Add Seat Stage");
                seatAnimator.SeatStageList[seatStageListProp.arraySize - 1].SetClassDefaults();
                serializedObject.Update();
            }
        }

        #endregion

        #region Protected Virtual Methods

        protected virtual void DrawBaseInspector()
        {
            #region Initialise
            seatAnimator.allowRepaint = false;
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
            EditorGUILayout.EndVertical();
            #endregion

            SSCEditorHelper.InTechPreview(false);

            EditorGUILayout.BeginVertical("HelpBox");

            DrawGeneralSettings();

            // Apply property changes
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.EndVertical();

            seatAnimator.allowRepaint = true;

            DrawDebugSettings();
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