using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
#if NET_LEGACY
using System.Linq;
#endif

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    [CustomEditor(typeof(AutoTargetingModule))]
    public class AutoTargetingModuleEditor : Editor
    {
        #region Custom Editor private variables
        private AutoTargetingModule autoTargetingModule;
        private bool isSceneModified = false;
        private bool isVerifyRequired = false;
        private bool isDebuggingEnabled = false;
        private bool isDebugShowQueryResults = false;

        // formatting and style variables
        private GUIStyle labelFieldRichText;
        private GUIStyle helpBoxRichText;
        private GUIStyle buttonCompact;
        private GUIStyle foldoutStyleNoLabel;
        private static GUIStyle toggleCompactButtonStyleNormal = null;  // Small Toggle button. e.g. G(izmo) on/off
        private static GUIStyle toggleCompactButtonStyleToggled = null;
        private float defaultEditorLabelWidth = 0f;
        private float defaultEditorFieldWidth = 0f;

        [System.NonSerialized] private SSCManager sscManager = null;

        #endregion

        #region GUIContent
        private readonly static GUIContent autoTargetingHeaderContent = new GUIContent("The Auto Targeting Module can be attached to a ShipControlModule or a Surface Turret Module. It works with the Radar system to automatically allocate targets within range of weapons.");
        private readonly static GUIContent initialiseOnStartContent = new GUIContent("Initialise On Start", "If enabled, the Initialise() will be called as soon as Start() runs. This should be disabled if you are initialising the autotargeting module through code and using the AutoTargetingModule API methods.");
        private readonly static GUIContent moduleModeContent = new GUIContent("Module Mode", "The mode that the AutoTargetingModule will operate in. This should match the module it is attached to.");
        private readonly static GUIContent isCheckLOSNewTargetContent = new GUIContent("Check LoS (New Target)", "When acquiring a new target, when enabled, this will verify there is a direct Line of Sight between the weapon and the target.");
        private readonly static GUIContent shipDisplayModuleContent = new GUIContent("Ship Display Module", "[OPTIONAL] If configured, targeting information can be set to this heads-up display in the scene");
        private readonly static GUIContent isTargetsShownOnHUDContent = new GUIContent("Show Targets on HUD", "Show the Targets on the heads-up display. Typically this is only used for player ships.");

        private readonly static GUIContent updateTargetPeriodicallyContent = new GUIContent("Update Target Periodically", "Whether the target should be reassigned periodically (after a fixed period of time).");
        private readonly static GUIContent updateTargetTimeContent = new GUIContent("Update Target Time", "The time to wait (in seconds) before assigning a new target.");

        private readonly static GUIContent canLoseTargetContent = new GUIContent("Can Lose Target", "Whether the current target can be 'lost' through either loss of line of sight or (for a turret) an inability to lock on to the target.");
        private readonly static GUIContent targetLostTimeContent = new GUIContent("Target Lost Time", "How long (in seconds) a target must be invalid for it to be lost (prompting a new target to be assigned).");
        private readonly static GUIContent isValidTargetRequireLOSContent = new GUIContent("Require LoS", "Whether a target can be 'lost' if line-of-sight is lost.");
        private readonly static GUIContent isValidTargetRequireTargetLockContent = new GUIContent("Require Target Lock", "Whether a target can be 'lost' if the turret is unable to lock on to it.");

        #endregion

        #region GUIContent - Debug
        private readonly static GUIContent debugModeContent = new GUIContent("Debug Mode", "Use this to troubleshoot the data at runtime in the editor.");
        private readonly static GUIContent debugIsInitialisedContent = new GUIContent("Is Initialised?");
        private readonly static GUIContent debugIsModuleModeValidContent = new GUIContent("Is Module Mode Valid?");
        private readonly static GUIContent debugIsTargetsShownOnHUDContent = new GUIContent("Targets Shown on HUD?");
        private readonly static GUIContent debugNumberOfTargetsContent = new GUIContent("Targets in Range");
        private readonly static GUIContent debugRadarQueryContent = new GUIContent("Current Radar Query");
        private readonly static GUIContent debugNoRadarQueryContent = new GUIContent(" None");
        private readonly static GUIContent debugRadarQueryFactionsToIncludeContent = new GUIContent(" Factions to Include");
        private readonly static GUIContent debugRadarQueryFactionsToExcludeContent = new GUIContent(" Factions to Exclude");
        private readonly static GUIContent debugRadarQuerySquadronsToIncludeContent = new GUIContent(" Squadrons to Include");
        private readonly static GUIContent debugRadarQuerySquadronsToExcludeContent = new GUIContent(" Squadrons to Exclude");
        private readonly static GUIContent debugRadarQueryRangeContent = new GUIContent(" Max Range");
        private readonly static GUIContent debugRadarNoContent = new GUIContent("---");
        private readonly static GUIContent debugRadarNoResultsContent = new GUIContent("Query returned no results");

        private readonly static GUIContent debugRadarShowQueryResultsContent = new GUIContent("Show Results (Expensive)", "Show the first 10 radar blips from the current query");

        #endregion

        #region Serialized Properties

        private SerializedProperty moduleModeProp;
        private SerializedProperty shipDisplayModuleProp;
        private SerializedProperty isTargetsShownOnHUDProp;
        private SerializedProperty updateTargetPeriodicallyProp;
        private SerializedProperty canLoseTargetProp;
        private SerializedProperty targetLostTimeProp;
        private SerializedProperty isValidTargetRequireLOSProp;
        private SerializedProperty isValidTargetRequireTargetLockProp;

        #endregion

        #region Events

        public void OnEnable()
        {
            autoTargetingModule = (AutoTargetingModule)target;

            #region Initialise properties
            moduleModeProp = serializedObject.FindProperty("moduleMode");
            shipDisplayModuleProp = serializedObject.FindProperty("shipDisplayModule");
            isTargetsShownOnHUDProp = serializedObject.FindProperty("isTargetsShownOnHUD");
            updateTargetPeriodicallyProp = serializedObject.FindProperty("updateTargetPeriodically");
            canLoseTargetProp = serializedObject.FindProperty("canLoseTarget");
            targetLostTimeProp = serializedObject.FindProperty("targetLostTime");
            isValidTargetRequireLOSProp = serializedObject.FindProperty("isValidTargetRequireLOS");
            isValidTargetRequireTargetLockProp = serializedObject.FindProperty("isValidTargetRequireTargetLock");

            #endregion

            defaultEditorLabelWidth = 185f; // EditorGUIUtility.labelWidth;
            defaultEditorFieldWidth = EditorGUIUtility.fieldWidth;

            // Reset GUIStyles
            helpBoxRichText = null;
            labelFieldRichText = null;
            buttonCompact = null;
            toggleCompactButtonStyleNormal = null;
            toggleCompactButtonStyleToggled = null;
            foldoutStyleNoLabel = null;

            if (autoTargetingModule != null) { autoTargetingModule.VerifyModule(); }
        }

        public void OnDestroy()
        {
            // Always unhide Unity tools when losing focus on this gameObject
            Tools.hidden = false;
        }

        /// <summary>
        /// Gets called automatically 10 times per second
        /// </summary>
        private void OnInspectorUpdate()
        {
            // OnInspectorGUI() only registers events when the mouse is positioned over the custom editor window
            // This code forces OnInspectorGUI() to run every frame, so it registers events even when the mouse
            // is positioned over the scene view
            if (autoTargetingModule.allowRepaint) { Repaint(); }
        }

        #endregion

        #region OnInspectorGUI

        // This function overides what is normally seen in the inspector window
        // This allows stuff like buttons to be drawn there
        public override void OnInspectorGUI()
        {
            if (autoTargetingModule == null) { return; }

            #region Initialise
            autoTargetingModule.allowRepaint = false;
            isSceneModified = false;
            EditorGUIUtility.labelWidth = defaultEditorLabelWidth;
            EditorGUIUtility.fieldWidth = defaultEditorFieldWidth;
            #endregion

            #region Configure Buttons and Styles

            if (helpBoxRichText == null)
            {
                helpBoxRichText = new GUIStyle("HelpBox");
                helpBoxRichText.richText = true;
            }

            if (buttonCompact == null)
            {
                buttonCompact = new GUIStyle("Button");
                buttonCompact.fontSize = 10;
            }

            if (labelFieldRichText == null)
            {
                labelFieldRichText = new GUIStyle("Label");
                labelFieldRichText.richText = true;
            }

            // Set up the toggle buttons styles
            if (toggleCompactButtonStyleNormal == null)
            {
                // Create a new button or else will effect the Button style for other buttons too
                toggleCompactButtonStyleNormal = new GUIStyle("Button");
                toggleCompactButtonStyleToggled = new GUIStyle(toggleCompactButtonStyleNormal);
                toggleCompactButtonStyleNormal.fontStyle = FontStyle.Normal;
                toggleCompactButtonStyleToggled.fontStyle = FontStyle.Bold;
                toggleCompactButtonStyleToggled.normal.background = toggleCompactButtonStyleToggled.active.background;
            }

            if (foldoutStyleNoLabel == null)
            {
                // When using a no-label foldout, don't forget to set the global
                // EditorGUIUtility.fieldWidth to a small value like 15, then back
                // to the original afterward.
                foldoutStyleNoLabel = new GUIStyle(EditorStyles.foldout);
                foldoutStyleNoLabel.fixedWidth = 0.01f;
            }
            #endregion

            #region Header Info
            SSCEditorHelper.SSCVersionHeader(labelFieldRichText);
            EditorGUILayout.LabelField(autoTargetingHeaderContent, helpBoxRichText);
            #endregion

            serializedObject.Update();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            #region General Settings
            EditorGUILayout.PropertyField(serializedObject.FindProperty("initialiseOnStart"), initialiseOnStartContent);

            if (!autoTargetingModule.IsModuleModeValid)
            {
                EditorGUILayout.HelpBox("The Module Mode needs to match the type of Module it is attached to.", MessageType.Error);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(moduleModeProp, moduleModeContent);
            if (EditorGUI.EndChangeCheck())
            {
                isVerifyRequired = true;
            }

            #endregion

            #region Surface Turret Module Mode
            if (moduleModeProp.intValue == (int)AutoTargetingModule.ModuleMode.SurfaceTurretModule)
            {

            }
            #endregion

            #region Ship Control Module Mode
            else if (moduleModeProp.intValue == (int)AutoTargetingModule.ModuleMode.ShipControlModule)
            {
                #region HUD
                EditorGUILayout.PropertyField(shipDisplayModuleProp, shipDisplayModuleContent);

                if (shipDisplayModuleProp.objectReferenceValue != null)
                {
                    EditorGUILayout.PropertyField(isTargetsShownOnHUDProp, isTargetsShownOnHUDContent);
                }

                #endregion
            }

            #endregion

            #region Missile Launcher Module Mode
            else if (moduleModeProp.intValue == (int)AutoTargetingModule.ModuleMode.MissileLaunchModule)
            {
                #if SCSM_SSCXP1

                #else
                SSCEditorHelper.SSCXPack1NotInstalled();
                #endif
            }
            #endregion

            #region Line of Sight Settings
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isCheckLineOfSightNewTarget"), isCheckLOSNewTargetContent);

            EditorGUILayout.PropertyField(updateTargetPeriodicallyProp, updateTargetPeriodicallyContent);
            if (updateTargetPeriodicallyProp.boolValue == true)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("updateTargetTime"), updateTargetTimeContent);
            }

            EditorGUILayout.PropertyField(canLoseTargetProp, canLoseTargetContent);
            if (canLoseTargetProp.boolValue)
            {
                EditorGUILayout.PropertyField(targetLostTimeProp, targetLostTimeContent);
                EditorGUILayout.PropertyField(isValidTargetRequireLOSProp, isValidTargetRequireLOSContent);
                EditorGUILayout.PropertyField(isValidTargetRequireTargetLockProp, isValidTargetRequireTargetLockContent);
            }
            // v1.4.5 RequireLOS or Target Lock should be off when canLoseTarget is off
            else if (isValidTargetRequireLOSProp.boolValue || isValidTargetRequireTargetLockProp.boolValue)
            {
                isValidTargetRequireLOSProp.boolValue = false;
                isValidTargetRequireTargetLockProp.boolValue = false;
            }
            #endregion

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();

            if (isVerifyRequired) { autoTargetingModule.VerifyModule(); isVerifyRequired = false; }

            #region Debug Mode
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            isDebuggingEnabled = EditorGUILayout.Toggle(debugModeContent, isDebuggingEnabled);
            // Debug at runtime in the editor

            if (isDebuggingEnabled && autoTargetingModule != null)
            {
                float rightLabelWidth = 150f;
                bool isInitialised = autoTargetingModule.IsInitialised;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(debugIsInitialisedContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
                EditorGUILayout.LabelField(isInitialised ? "Yes" : "No", GUILayout.MaxWidth(rightLabelWidth));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(debugIsModuleModeValidContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
                EditorGUILayout.LabelField(autoTargetingModule.IsModuleModeValid ? "Yes" : "No", GUILayout.MaxWidth(rightLabelWidth));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(debugNumberOfTargetsContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
                EditorGUILayout.LabelField(autoTargetingModule.NumberOfTargetsInRange.ToString(), GUILayout.MaxWidth(rightLabelWidth));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(debugIsTargetsShownOnHUDContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
                EditorGUILayout.LabelField(autoTargetingModule.IsTargetsShownOnHUD ? "Yes" : "No", GUILayout.MaxWidth(rightLabelWidth));
                EditorGUILayout.EndHorizontal();

                SSCRadarQuery currentQuery = autoTargetingModule.GetCurrentQuery;

                #region Current Query
                EditorGUILayout.LabelField(debugRadarQueryContent);
                if (currentQuery != null)
                {
                    // Scripting runtime version .NET 3.5 Equivalent (NET_LEGACY) does not support string.Join(sep, int[]).
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(debugRadarQueryFactionsToIncludeContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
                    #if NET_LEGACY
                    EditorGUILayout.LabelField(currentQuery.factionsToInclude == null ? "-" : string.Join(",", currentQuery.factionsToInclude.Select(x=>x.ToString()).ToArray()));
                    #else
                    EditorGUILayout.LabelField(currentQuery.factionsToInclude == null ? "-" : string.Join(",", currentQuery.factionsToInclude));
                    #endif
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(debugRadarQuerySquadronsToIncludeContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
                    #if NET_LEGACY
                    EditorGUILayout.LabelField(currentQuery.squadronsToInclude == null ? "-" : string.Join(",", currentQuery.squadronsToInclude.Select(x=>x.ToString()).ToArray()));
                    #else
                    EditorGUILayout.LabelField(currentQuery.squadronsToInclude == null ? "-" : string.Join(",", currentQuery.squadronsToInclude));
                    #endif
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(debugRadarQueryFactionsToExcludeContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
                    #if NET_LEGACY
                    EditorGUILayout.LabelField(currentQuery.factionsToExclude == null ? "-" : string.Join(",", currentQuery.factionsToExclude.Select(x=>x.ToString()).ToArray()));
                    #else
                    EditorGUILayout.LabelField(currentQuery.factionsToExclude == null ? "-" : string.Join(",", currentQuery.factionsToExclude));
                    #endif
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(debugRadarQuerySquadronsToExcludeContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
                    #if NET_LEGACY
                    EditorGUILayout.LabelField(currentQuery.squadronsToExclude == null ? "-" : string.Join(",", currentQuery.squadronsToExclude.Select(x=>x.ToString()).ToArray()));
                    #else
                    EditorGUILayout.LabelField(currentQuery.squadronsToExclude == null ? "-" : string.Join(",", currentQuery.squadronsToExclude));
                    #endif
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(debugRadarQueryRangeContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
                    EditorGUILayout.LabelField(currentQuery.range.ToString("0.0"));
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.LabelField(debugNoRadarQueryContent);
                }
                #endregion

                #region Query Results
                isDebugShowQueryResults = EditorGUILayout.Toggle(debugRadarShowQueryResultsContent, isDebugShowQueryResults);
                if (isDebugShowQueryResults)
                {
                    EditorGUI.indentLevel++;

                    SSCEditorHelper.PerformanceImpact();

                    List<SSCRadarBlip> blips = autoTargetingModule.GetBlipList;

                    int numBlips = blips == null ? 0 : blips.Count;

                    if (numBlips == 0)
                    {
                        EditorGUILayout.LabelField(debugRadarNoResultsContent);
                    }

                    // Display only the first 20 blips
                    for (int bIdx = 0; bIdx < numBlips && bIdx < 20; bIdx++)
                    {
                        SSCRadarBlip blip = blips[bIdx];
                        var radarItemType = blip.radarItemType;

                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.LabelField(radarItemType.ToString(), GUILayout.Width(150f));

                        if (radarItemType == SSCRadarItem.RadarItemType.AIShip || radarItemType == SSCRadarItem.RadarItemType.PlayerShip)
                        {
                            if (blip.shipControlModule == null)
                            {
                                EditorGUILayout.LabelField(debugRadarNoContent);
                            }
                            else
                            {
                                EditorGUILayout.LabelField(blip.shipControlModule.name);
                            }
                        }
                        else if (radarItemType == SSCRadarItem.RadarItemType.GameObject)
                        {
                            if (blip.itemGameObject == null)
                            {
                                EditorGUILayout.LabelField(debugRadarNoContent);
                            }
                            else
                            {
                                EditorGUILayout.LabelField(blip.itemGameObject.name);
                            }
                        }
                        else if (radarItemType == SSCRadarItem.RadarItemType.Custom)
                        {
                            EditorGUILayout.LabelField(debugRadarNoContent);
                        }
                        else if (radarItemType == SSCRadarItem.RadarItemType.Location)
                        {
                            if (!isInitialised)
                            {
                                EditorGUILayout.LabelField("---");
                            }
                            else if (sscManager == null)
                            {
                                // Get the SSCManager for use with the next frame
                                // If there is a location on radar, we "should" also have a SSCManager in this scene.
                                sscManager = SSCManager.GetOrCreateManager(autoTargetingModule.gameObject.scene.handle);
                                EditorGUILayout.LabelField("---");
                            }
                            else
                            {
                                LocationData location = sscManager.GetLocation(blip.guidHash);
                                if (location != null)
                                {
                                    EditorGUILayout.LabelField(string.IsNullOrEmpty(location.name) ? "No Name" : location.name);
                                }
                                else
                                {
                                    // For some reason couldn't find the location - maybe radar in the wrong scene??
                                    EditorGUILayout.LabelField(blip.guidHash.ToString() + " (not found in same scene)");
                                }
                            }
                        }
                        else if (radarItemType == SSCRadarItem.RadarItemType.ShipDamageRegion)
                        {
                            EditorGUILayout.LabelField(blip.itemGameObject == null ? (blip.shipControlModule == null ? "---" : blip.shipControlModule.name) : blip.itemGameObject.name);
                        }
                        else
                        {
                            EditorGUILayout.LabelField(debugRadarNoContent);
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUI.indentLevel--;
                }
                #endregion
            }

            EditorGUILayout.EndVertical();
            #endregion

            #region Mark Scene Dirty if required

            if (isSceneModified && !Application.isPlaying)
            {
                isSceneModified = false;
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            #endregion

            autoTargetingModule.allowRepaint = true;
        }

        /// <summary>
        /// Force the inspector to refresh each frame when debugging is enabled with show query results
        /// </summary>
        /// <returns></returns>
        public override bool RequiresConstantRepaint()
        {
            return autoTargetingModule != null && isDebuggingEnabled && isDebugShowQueryResults;
        }

        #endregion
    }
}