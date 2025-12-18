using UnityEditor;
using UnityEngine;
using SciFiShipController;

// Sci-Fi Ship Controller. Copyright (c) 2018-2025 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipControllerSample
{
    [CustomEditor(typeof(SampleShipInherit))]
    public class SampleShipInheritEditor : ShipControlModuleEditor
    {
        #region Private Methods

        /// <summary>
        /// Modify this for your own custom fields
        /// </summary>
        private void DrawSampleFields()
        {          
            serializedObject.Update();
            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.LabelField(new GUIContent("My Extra Data"));

            // Display custom fields here
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mySampleVariable"), new GUIContent("MySampleVariable"));

            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region OnInspectorGUI

        public override void OnInspectorGUI()
        {
            #if !SCSM_SSC
            EditorGUILayout.HelpBox("Sci-Fi Ship Controller asset is missing from the project.", MessageType.Error);
            #else
            DrawBaseInspector();

            DrawSampleFields();
            #endif
        }

        #endregion
    }
}