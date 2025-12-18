using UnityEditor;
using UnityEngine;

// Sci-Fi Ship Controller. Copyright (c) 2018-2025 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    [CustomEditor(typeof(SSCLookup))]
    public class SSCLookupEditor : Editor
    {
        #region GUIContent - Headers
        private readonly static GUIContent defaultContent = new GUIContent("Default");
        private readonly static GUIContent titleContent = new GUIContent("Lookup");
        private readonly static GUIContent headerContent = new GUIContent("Contains common lookup items");
        #endregion

        #region Serialized Properties - General
        private SerializedProperty lookupValuesProp;
        private SerializedProperty isExpandedInEditorProp;
        #endregion

        #region Custom Editor protected variables
        // These are visible to inherited classes
        protected SSCLookup sscLookup;
        protected float elementLabelWidth;
        protected string elementPrefix;
        protected GUIContent shortNameContent = new GUIContent();

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
        protected GUIStyle miniLabelWrappedText;
        protected GUIStyle foldoutStyleNoLabel;
        protected GUIStyle helpLabelRichText;
        protected GUIStyle toggleCompactButtonStyleNormal = null;  // Small Toggle button. e.g. G(izmo) on/off
        protected GUIStyle toggleCompactButtonStyleToggled = null;
        protected Color separatorColor = new Color();
        #endregion

        #region Events

        public virtual void OnEnable()
        {
            sscLookup = (SSCLookup)target;

            defaultEditorLabelWidth = 150f; // EditorGUIUtility.labelWidth;
            defaultEditorFieldWidth = EditorGUIUtility.fieldWidth;

            separatorColor = EditorGUIUtility.isProSkin ? new Color(0.2f, 0.2f, 0.2f, 2f) : Color.grey;

            // Reset GUIStyles
            isStylesInitialised = false;
            toggleCompactButtonStyleNormal = null;
            toggleCompactButtonStyleToggled = null;
            foldoutStyleNoLabel = null;

            #region Find Properties - General
            lookupValuesProp = serializedObject.FindProperty("lookupValues");
            isExpandedInEditorProp = serializedObject.FindProperty("isExpandedInEditor");
            #endregion

            elementLabelWidth = sscLookup.ElementLabelWidth;
            if (elementLabelWidth < 10f) { elementLabelWidth = 80f; }
            elementPrefix = sscLookup.ElementPrefix;
            shortNameContent.text = sscLookup.LookupsShortName;
        }

        #endregion

        #region Protected Virtual Methods

        /// <summary>
        /// Draw the base inspector
        /// </summary>
        protected virtual void DrawBaseInspector()
        {
            #region Initialise
            EditorGUIUtility.labelWidth = defaultEditorLabelWidth;
            EditorGUIUtility.fieldWidth = defaultEditorFieldWidth;
            #endregion

            ConfigureButtonsAndStyles();

            #region Header Info and Buttons
            SSCEditorHelper.SSCVersionHeader(labelFieldRichText);
            GUILayout.BeginVertical("HelpBox");
            DrawTitleContent();
            DrawHeaderContent();
            GUILayout.EndVertical();
            #endregion

            // Read in all the properties
            serializedObject.Update();

            GUILayout.BeginVertical("HelpBox");
            DrawDefaultLookup();
            int arraySize = lookupValuesProp.arraySize;
            SSCEditorHelper.DrawArray(lookupValuesProp, isExpandedInEditorProp, shortNameContent, elementLabelWidth, elementPrefix, buttonCompact, foldoutStyleNoLabel, defaultEditorFieldWidth);
            if (arraySize != lookupValuesProp.arraySize)
            {
                EditorUtility.SetDirty(sscLookup);
            }
            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draw the header in the inspector
        /// </summary>
        protected virtual void DrawHeaderContent()
        {
            EditorGUILayout.LabelField(headerContent, miniLabelWrappedText);
        }

        /// <summary>
        /// Draw the title in the inspector
        /// </summary>
        protected virtual void DrawTitleContent()
        {
            EditorGUILayout.LabelField(titleContent);
        }

        #endregion

        #region Protected Non-Virtual Methods

        protected void ConfigureButtonsAndStyles()
        {
            // Set up rich text GUIStyles
            if (!isStylesInitialised)
            {
                helpBoxRichText = new GUIStyle("HelpBox");
                helpBoxRichText.richText = true;

                labelFieldRichText = new GUIStyle("Label");
                labelFieldRichText.richText = true;

                miniLabelWrappedText = new GUIStyle(EditorStyles.miniLabel);
                miniLabelWrappedText.richText = true;
                miniLabelWrappedText.wordWrap = true;

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

        /// <summary>
        /// Draw the default Lookup value in the inspector
        /// </summary>
        protected void DrawDefaultLookup()
        {
            EditorGUILayout.BeginHorizontal();
            SSCEditorHelper.DrawSSCLabelIndent(12f, defaultContent, sscLookup.ElementLabelWidth - 8f);
            //EditorGUILayout.LabelField(defaultContent, GUILayout.Width(defaultEditorLabelWidth));
            EditorGUILayout.LabelField(sscLookup.DefaultValue);
            EditorGUILayout.EndHorizontal();
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