using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    [CustomEditor(typeof(DemoControlModule))]
    public class DemoControlModuleEditor : Editor
    {
        #region Custom Editor private variables

        // Formatting and style variables
        //private string txtColourName = "Black";
        private Color defaultTextColour = Color.black;
        private string labelText;
        private GUIStyle labelFieldRichText;
        private GUIStyle helpBoxRichText;
        private GUIStyle buttonCompact;
        private float defaultEditorLabelWidth = 0f;
        private float defaultEditorFieldWidth = 0f;

        private bool isSceneModified = false;       // used in OnInspectorGUI()
        private DemoControlModule demoControlModule = null;

        // SceneGUI variables
        private Vector3 componentHandlePosition = Vector3.zero;
        private Quaternion componentHandleRotation = Quaternion.identity;
        private bool isSceneViewModified = false;   // Used in SceneGUI(SceneView sv)
        private Squadron svSquadron = null;
        private Vector3 scale = Vector3.zero;
        private Vector3 theatrePos = Vector3.zero;

        #endregion

        #region GUIContent - Squadrons
        private readonly static GUIContent headerContent = new GUIContent("<b>Demo Control Module</b>\n\nThis module demonstrates how to spawn squadrons of ships");
        private readonly static GUIContent squadronHeaderContent = new GUIContent("Squadrons are groups of typically the same ship type. They can contain 0, 1 or more ships from the same faction or alliance.");
        private readonly static GUIContent squadronContent = new GUIContent("Squadrons");
        private readonly static GUIContent squadronIdContent = new GUIContent("Squadron Id", "The unique number or ID for this squadron");
        private readonly static GUIContent squadronNameContent = new GUIContent("Squadron Name");
        private readonly static GUIContent factionIdContent = new GUIContent("Faction Id", "The Faction that the squadron belongs to or fights for.");
        private readonly static GUIContent anchorPositionContent = new GUIContent("Anchor Position", "The initial front middle position of the squadron. If there is more than one row on y-axis, rows will be created above this position.");
        private readonly static GUIContent fwdDirectionContent = new GUIContent("Forward Direction", "Direction as a normalised vector");
        private readonly static GUIContent anchorRotationContent = new GUIContent("Anchor Rotation", "The forward direction as euler angles. This is modified by setting the Forward Direction vector");
        private readonly static GUIContent tacticalFormationContent = new GUIContent("Tactical Formation", "The type of formation in which to spawn ships");
        private readonly static GUIContent rowsXContent = new GUIContent("Rows on x-axis", "The number of rows along the x-axis");
        private readonly static GUIContent rowsZContent = new GUIContent("Rows on z-axis", "The number of rows along the z-axis");
        private readonly static GUIContent rowsYContent = new GUIContent("Rows on y-axis", "The number of rows along the y-axis");
        private readonly static GUIContent offsetXContent = new GUIContent("Row offset x-axis", "The distance between rows on the x-axis");
        private readonly static GUIContent offsetZContent = new GUIContent("Row offset z-axis", "The distance between rows on the z-axis");
        private readonly static GUIContent offsetYContent = new GUIContent("Row offset y-axis", "The distance between rows on the y-axis");
        private readonly static GUIContent shipPrefabContent = new GUIContent("NPC Ship Prefab", "Non-Player-Character ship which will be used to populate the squadron");
        private readonly static GUIContent playerShipContent = new GUIContent("Player Ship", "Optionally reference to a player ship in the scene to lead this squadron");
        private readonly static GUIContent cameraTargetOffsetContent = new GUIContent("Camera Ship Offset", "The offset from the ship (in local space) for the camera to aim for.");
        #endregion

        #region GUIContent - AI Targets
        private readonly static GUIContent assignAITargetsContent = new GUIContent("Assign AI Targets","");
        private readonly static GUIContent aiTargetsContent = new GUIContent("AI Targets","");
        private readonly static GUIContent reassignTargetSecsContent = new GUIContent("Reassign Target Secs", "");
        private readonly static GUIContent AddAIScriptIfMissingContent = new GUIContent("Add AI Script If Missing", "");
        private readonly static GUIContent CrashAffectsHealthContent = new GUIContent("Crash Affects Health", "");
        private readonly static GUIContent theatreBoundsContent = new GUIContent("Theatre Bounds", "The region that the ships can fly or operate in. Extents are distance from centre.");

        #endregion

        #region GUIContent - Radar
        private readonly static GUIContent useRadarContent = new GUIContent("Use Radar", "Enable radar tracking for all ships");

        #endregion

        #region Serialized Properties

        private SerializedProperty squadronListProp;
        private SerializedProperty squadronProp;
        private SerializedProperty squadronIdProp;
        private SerializedProperty squadronNameProp;
        private SerializedProperty squadronShowInEditorProp;
        private SerializedProperty squadronTacticalFormationProp;
        private SerializedProperty squadronFwdDirectionProp;

        #endregion

        #region Event Methods

        private void OnEnable()
        {
            demoControlModule = (DemoControlModule)target;
            squadronListProp = serializedObject.FindProperty("squadronList");

            #if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= SceneGUI;
            SceneView.duringSceneGui += SceneGUI;
            #else
            SceneView.onSceneGUIDelegate -= SceneGUI;
            SceneView.onSceneGUIDelegate += SceneGUI;
            #endif

            Tools.hidden = true;

            // Used in Richtext labels
            //if (EditorGUIUtility.isProSkin) { txtColourName = "White"; defaultTextColour = new Color(180f / 255f, 180f / 255f, 180f / 255f, 1f); }

            // Keep compiler happy - can remove this later if it isn't required
            if (defaultTextColour.a > 0f) { }
        }

        private void OnDisable()
        {
            Tools.hidden = false;
            Tools.current = Tool.Move;

            #if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= SceneGUI;
            #else
            SceneView.onSceneGUIDelegate -= SceneGUI;
            #endif
        }

        private void OnDestroy()
        {
            Tools.hidden = false;
            Tools.current = Tool.Move;
        }

        #endregion

        #region OnInspectorGUI

        // This function overides what is normally seen in the inspector window
        // This allows stuff like buttons to be drawn there
        public override void OnInspectorGUI()
        {
            #region Initialise

            EditorGUIUtility.labelWidth = defaultEditorLabelWidth;
            EditorGUIUtility.fieldWidth = defaultEditorFieldWidth;

            #endregion

            #region Configure Buttons and Styles

            // Set up rich text GUIStyles
            helpBoxRichText = new GUIStyle("HelpBox");
            helpBoxRichText.richText = true;

            labelFieldRichText = new GUIStyle("Label");
            labelFieldRichText.richText = true;

            buttonCompact = new GUIStyle("Button");
            buttonCompact.fontSize = 10;

            #endregion

            // Read in all the properties
            serializedObject.Update();

            SSCEditorHelper.SSCVersionHeader(labelFieldRichText);
            EditorGUILayout.LabelField(headerContent, helpBoxRichText);

            #region Squadrons

            EditorGUILayout.LabelField(squadronHeaderContent, helpBoxRichText);

            if (squadronListProp == null)
            {
                // Apply property changes
                serializedObject.ApplyModifiedProperties();
                demoControlModule.squadronList = new List<Squadron>();
                isSceneModified = true;
                // Read in the properties
                serializedObject.Update();
            }

            EditorGUILayout.PropertyField(squadronListProp, squadronContent);

            #endregion

            #region AI Targets

            GUILayout.BeginVertical("HelpBox");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("assignAITargets"), assignAITargetsContent);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("aiTargets"), aiTargetsContent);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("reassignTargetSecs"), reassignTargetSecsContent);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AddAIScriptIfMissing"), AddAIScriptIfMissingContent);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("HelpBox");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CrashAffectsHealth"), CrashAffectsHealthContent);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("theatreBounds"), theatreBoundsContent);
            GUILayout.EndVertical();

            #endregion

            #region Radar
            GUILayout.BeginVertical("HelpBox");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useRadar"), useRadarContent);
            GUILayout.EndVertical();
            #endregion

            // Apply property changes
            serializedObject.ApplyModifiedProperties();

            #region Mark Scene Dirty if required

            if (isSceneModified && !Application.isPlaying)
            {
                isSceneModified = false;
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            #endregion
        }
        #endregion

        #region Private Methods

        private void SceneGUI(SceneView sv)
        {
            // Only display
            if (demoControlModule != null)
            {
                int numSquadrons = demoControlModule.squadronList == null ? 0 : demoControlModule.squadronList.Count;

                isSceneViewModified = false;

                for (int sqIdx = 0; sqIdx < numSquadrons; sqIdx++)
                {
                    svSquadron = demoControlModule.squadronList[sqIdx];
                    componentHandlePosition = svSquadron.anchorPosition;
                    componentHandleRotation = Quaternion.LookRotation(svSquadron.fwdDirection, Vector3.up);

                    // Draw a sphere in the scene that is non-interactable
                    if (Event.current.type == EventType.Repaint)
                    {
                        using (new Handles.DrawingScope(Color.yellow))
                        {
                            Handles.SphereHandleCap(0, componentHandlePosition, componentHandleRotation, 3f, EventType.Repaint);
                        }
                    }

                    // Only display handle if the squadron is expanded in the editor
                    if (svSquadron.showInEditor)
                    {
                        if (Tools.current == Tool.Rotate)
                        {
                            EditorGUI.BeginChangeCheck();

                            // Draw a rotation handle
                            componentHandleRotation = Handles.RotationHandle(componentHandleRotation, componentHandlePosition);

                            // Use the rotation handle to edit the direction of thrust
                            if (EditorGUI.EndChangeCheck())
                            {
                                isSceneViewModified = true;
                                Undo.RecordObject(demoControlModule, "Rotate Squadron Anchor Position");

                                // ================================
                                // TODO - THIS MAY BE WRONG....
                                // ================================
                                svSquadron.fwdDirection = componentHandleRotation * Vector3.forward;
                            }
                        }

                        #if UNITY_2017_3_OR_NEWER
                        else if (Tools.current == Tool.Move || Tools.current == Tool.Transform)
                        #else
                        else if (Tools.current == Tool.Move)
                        #endif
                        {
                            EditorGUI.BeginChangeCheck();
                            componentHandlePosition = Handles.PositionHandle(componentHandlePosition, componentHandleRotation);
                            if (EditorGUI.EndChangeCheck())
                            {
                                isSceneViewModified = true;
                                Undo.RecordObject(demoControlModule, "Move Squadron Anchor Position");
                                svSquadron.anchorPosition = componentHandlePosition;
                            }
                        }

                    }
                }

                // Draw the theatre of operations boundaries
                if (demoControlModule.theatreBounds.extents != Vector3.zero)
                {
                    Handles.DrawWireCube(demoControlModule.theatreBounds.center, demoControlModule.theatreBounds.extents * 2f);
                }

                if (Tools.current == Tool.Scale)
                {
                    scale = demoControlModule.theatreBounds.extents;
                    theatrePos = demoControlModule.theatreBounds.center;
                    EditorGUI.BeginChangeCheck();
                    scale = Handles.ScaleHandle(scale, theatrePos, Quaternion.identity, HandleUtility.GetHandleSize(theatrePos));
                    if (EditorGUI.EndChangeCheck())
                    {
                        demoControlModule.theatreBounds.extents = scale;
                        GUI.FocusControl(null);
                        Undo.RecordObject(demoControlModule, "Modify Theatre Extents");
                        isSceneViewModified = true;
                    }
                }

                #region Mark Scene Dirty if required

                if (isSceneViewModified && !Application.isPlaying)
                {
                    isSceneViewModified = false;
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }

                #endregion
            }
        }

        #endregion
    }
}
