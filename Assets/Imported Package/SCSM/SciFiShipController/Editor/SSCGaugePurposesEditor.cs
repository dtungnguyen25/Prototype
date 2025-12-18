using UnityEditor;
using UnityEngine;

// Sci-Fi Ship Controller. Copyright (c) 2018-2025 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    [CustomEditor(typeof(SSCGaugePurposes))]
    public class SSCGaugePurposesEditor : SSCLookupEditor
    {
        #region GUIContent - Headers
        private readonly static GUIContent titleContent = new GUIContent("Gauge Purposes");
        private readonly static GUIContent headerContent = new GUIContent("Contains common Ship Display Module gauge purposes");
        #endregion

        #region Protected Methods

        protected override void DrawHeaderContent()
        {
            EditorGUILayout.LabelField(headerContent, miniLabelWrappedText);
        }


        protected override void DrawTitleContent()
        {
            EditorGUILayout.LabelField(titleContent);
        }

        #endregion

    }
}