using UnityEngine;
using UnityEditor;

// Sci-Fi Ship Controller. Copyright (c) 2018-2025 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    [CustomEditor(typeof(ProjectileModule))]
    public class ProjectileModuleEditor : Editor
    {
        #region Custom Editor protected variables
        // These are visible to inherited classes

        protected ProjectileModule projectileModule;

        // Formatting and style variables
        protected string labelText;
        protected GUIStyle labelFieldRichText;
        protected GUIStyle helpBoxRichText;
        protected GUIStyle buttonCompact;
        protected float defaultEditorLabelWidth = 0f;
        protected float defaultEditorFieldWidth = 0f;
        protected bool isDebuggingEnabled = false;
        #endregion

        #region GUIContent

        private readonly static GUIContent headerContent = new GUIContent("<b>Projectile Module</b>\n\nThis module enables you to implement projectile behaviour on the object it is attached to.");
        protected readonly static GUIContent startSpeedContent = new GUIContent("Start Speed", "The starting speed of the projectile or missile when it is launched.");
        protected readonly static GUIContent useGravityContent = new GUIContent("Use Gravity", "Whether gravity is applied to the projectile or missile. If applied, the amount and direction of gravity will be inherited from the ship or weapon that fired it.");
        protected readonly static GUIContent damageTypeContent = new GUIContent("Damage Type", "The type of damage the projectile or missile does when hitting a ship. " +
            "The amount of damage dealt to a ship upon collision is dependent on the ship's resistance to this damage type. If the " +
            "damage type is set to Default, the ship's damage multipliers are ignored i.e. the damage amount is unchanged.");
        protected readonly static GUIContent damageAmountContent = new GUIContent("Damage Amount", "The amount of damage the projectile or missile does on collision with a ship or object. NOTE: Non-ship objects need a DamageReceiver component.");
        protected readonly static GUIContent isDamageSourceContent = new GUIContent("Damage Source", "If a projectile (or missile) hits the source ship that fired it, should it incur damage?");
        protected readonly static GUIContent collisionLayerMaskContent = new GUIContent("Collision Mask", "The layer mask used for collision testing for this projectile or missile. Default: Everything");
        protected readonly static GUIContent useECSContent = new GUIContent("Use DOTS", "Use Data-Oriented Technology Stack which uses the Entity Component System and Job System to create and destroy projectiles. Has no effect if Unity 2019.1, ECS, and Jobs is not installed.");
        protected readonly static GUIContent usePoolingContent = new GUIContent("Use Pooling", "Use the Pooling system to manage create, re-use, and destroy projectiles.");
        protected readonly static GUIContent minPoolSizeContent = new GUIContent("Min Pool Size", "When using the Pooling system, this is the number of projectile or missile objects kept in reserve for spawning and despawning.");
        protected readonly static GUIContent maxPoolSizeContent = new GUIContent("Max Pool Size", "When using the Pooling system, this is the maximum number of projectiles or missiles permitted in the scene at any one time.");
        protected readonly static GUIContent despawnTimeContent = new GUIContent("Despawn Time", "If the projectile or missile has not collided with something before this time (in seconds), it is automatically despawned or removed from the scene.");
        protected readonly static GUIContent isKinematicGuideToTargetContent = new GUIContent("Guide to Target", "Rather than being fire and forget, is this projectile or missile guided to a target with kinematics?");
        protected readonly static GUIContent guidedMaxTurnSpeedContent = new GUIContent("Guided Max Turn Speed", "The max turning speed in degrees per second for a guided projectile or missile.");
        protected readonly static GUIContent effectsObjectContent = new GUIContent("Destroyed FX Object", "The particle and/or sound effect prefab that will be instantiated when the projectile or missile hits something and is destroyed. This does not fire when the projectile or missile is automatically despawned.");
        protected readonly static GUIContent shieldEffectsObjectContent = new GUIContent("Shield FX Object", "The particle and/or sound effect prefab that will be instantiated, instead of the regular Effects Object, when the projectile or missile hits a shielded ship. This does not fire when the projectile or missile is automatically despawned.");
        protected readonly static GUIContent shieldPenetrationContent = new GUIContent("Shield Penetration", "The amount this projectile (or missile from SSC Expansion Pack 1), can penetrate a shield on impact.");
        protected readonly static GUIContent muzzleFXObjectContent = new GUIContent("Muzzle FX Object", "The particle and/or sound effect prefab that will be instantiated when the projectile or missile is fired from a weapon.");
        protected readonly static GUIContent muzzleFXOffsetContent = new GUIContent("Muzzle FX Offset", "The distance in local space that the muzzle Effects Object should be instantiated from the weapon firing point. Typically only the z-axis will be used.");
        protected readonly static GUIContent gotoEffectFolderBtnContent = new GUIContent("F", "Find and highlight the sample Effects folder");
        #endregion

        #region GUIContent - Debug
        private readonly static GUIContent debugModeContent = new GUIContent("Debug Mode", "Use this to troubleshoot the data at runtime in the editor.");
        private readonly static GUIContent debugSourceShipIdContent = new GUIContent("Source Ship Id", "The ShipId of the ship that fired this projectile or missile");
        private readonly static GUIContent debugSourceSquadronIdContent = new GUIContent("Source Squadron Id", "The Squadron which the ship belonged to when it fired the projectile or missile");
        private readonly static GUIContent debugEstimatedRangeContent = new GUIContent("Estimated Range", "The estimated range (in metres) of this projectile assuming it travels at a constant velocity");
        private readonly static GUIContent debugVelocityContent = new GUIContent("Current Velocity", "Current velocity of the projectile or missile.");
        private readonly static GUIContent debugLocalVelocityContent = new GUIContent("Local Velocity", "Current local space velocity of the projectile or missile.");
        private readonly static GUIContent debugSpeedContent = new GUIContent("Speed km/h");
        private readonly static GUIContent debugTargetShipContent = new GUIContent("Target Ship", "If a ship is being targeted, will return its name. If it is being targeted but is NULL, will assume destroyed.");
        private readonly static GUIContent debugTargetShipDamageRegionContent = new GUIContent("Target Damage Region", "If a ship's damage region is being targeted, will return its name. If it is being targeted but the ship is NULL, will assume destroyed.");
        private readonly static GUIContent debugTargetGameObjectContent = new GUIContent("Target GameObject", "If a gameobject is being targeted, will return its name");
        private readonly static GUIContent debugTargetLocationContent = new GUIContent("Target Location", "If a location is being targeted, will return its name");
        #endregion

        #region Serialized Properties

        protected SerializedProperty startSpeedProp;
        protected SerializedProperty useGravityProp;
        protected SerializedProperty damageTypeProp;
        protected SerializedProperty damageAmountProp;
        protected SerializedProperty isDamageSourceProp;
        protected SerializedProperty useECSProp;
        protected SerializedProperty usePoolingProp;
        protected SerializedProperty minPoolSizeProp;
        protected SerializedProperty maxPoolSizeProp;
        protected SerializedProperty isKinematicGuideToTargetProp;
        protected SerializedProperty guidedMaxTurnSpeedProp;
        protected SerializedProperty effectsObjectProp;
        protected SerializedProperty shieldEffectsObjectProp;
        protected SerializedProperty shieldPenetrationProp;
        protected SerializedProperty muzzleEffectsObjectProp;
        protected SerializedProperty collisionLayerMaskProp;

        #endregion

        #region Events

        public virtual void OnEnable()
        {
            projectileModule = (ProjectileModule)target;

            defaultEditorLabelWidth = 150f; // EditorGUIUtility.labelWidth;
            defaultEditorFieldWidth = EditorGUIUtility.fieldWidth;

            #region Find Properties
            startSpeedProp = serializedObject.FindProperty("startSpeed");
            useGravityProp = serializedObject.FindProperty("useGravity");
            damageTypeProp = serializedObject.FindProperty("damageType");
            damageAmountProp = serializedObject.FindProperty("damageAmount");
            isDamageSourceProp = serializedObject.FindProperty("isDamageSource");
            collisionLayerMaskProp = serializedObject.FindProperty("collisionLayerMask");
            useECSProp = serializedObject.FindProperty("useECS");
            usePoolingProp = serializedObject.FindProperty("usePooling");
            isKinematicGuideToTargetProp = serializedObject.FindProperty("isKinematicGuideToTarget");
            guidedMaxTurnSpeedProp = serializedObject.FindProperty("guidedMaxTurnSpeed");
            effectsObjectProp = serializedObject.FindProperty("effectsObject");
            shieldEffectsObjectProp = serializedObject.FindProperty("shieldEffectsObject");
            shieldPenetrationProp = serializedObject.FindProperty("shieldPenetration");
            muzzleEffectsObjectProp = serializedObject.FindProperty("muzzleEffectsObject");

            #endregion
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Draw the the basic debugging items in the inspector
        /// </summary>
        /// <param name="rightLabelWidth"></param>
        protected void DrawDebugBaseContent(float rightLabelWidth)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(debugSourceShipIdContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
            EditorGUILayout.LabelField(projectileModule.sourceShipId.ToString(), GUILayout.MaxWidth(rightLabelWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(debugSourceSquadronIdContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
            EditorGUILayout.LabelField(projectileModule.sourceSquadronId.ToString(), GUILayout.MaxWidth(rightLabelWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(debugEstimatedRangeContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
            EditorGUILayout.LabelField(projectileModule.estimatedRange.ToString("0.0"), GUILayout.MaxWidth(rightLabelWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(debugVelocityContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
            EditorGUILayout.LabelField(projectileModule.Velocity.ToString(), GUILayout.MaxWidth(rightLabelWidth));
            EditorGUILayout.EndHorizontal();

            Vector3 localVelo = projectileModule.LocalVelocity;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(debugLocalVelocityContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
            EditorGUILayout.LabelField(localVelo.ToString(), GUILayout.MaxWidth(rightLabelWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(debugSpeedContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth));
            // z-axis of Local space velocity. Convert to km/h
            EditorGUILayout.LabelField((localVelo.z * 3.6f).ToString("0.0") + " (m/s " + localVelo.z.ToString("0.0") + ")", GUILayout.MaxWidth(rightLabelWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(debugTargetShipContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
            EditorGUILayout.LabelField(projectileModule.TargetShipName, GUILayout.MaxWidth(rightLabelWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(debugTargetShipDamageRegionContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
            EditorGUILayout.LabelField(projectileModule.TargetShipDamageRegionName, GUILayout.MaxWidth(rightLabelWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(debugTargetGameObjectContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
            EditorGUILayout.LabelField(projectileModule.TargetGameObjectName, GUILayout.MaxWidth(rightLabelWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(debugTargetLocationContent, labelFieldRichText, GUILayout.Width(defaultEditorLabelWidth - 3f));
            EditorGUILayout.LabelField(projectileModule.TargetLocationName, GUILayout.MaxWidth(rightLabelWidth));
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw the effects object in the inspector
        /// </summary>
        protected void DrawEffectsObject()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(effectsObjectContent, GUILayout.Width(defaultEditorLabelWidth - 24f));
            if (GUILayout.Button(gotoEffectFolderBtnContent, buttonCompact, GUILayout.Width(20f))) { SSCEditorHelper.HighlightFolderInProjectWindow(SSCSetup.effectsFolder, false, true); }
            EditorGUILayout.PropertyField(effectsObjectProp, GUIContent.none);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw muzzle (flash) effects object in the inspector
        /// </summary>
        protected void DrawMuzzleEffectsObject()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(muzzleFXObjectContent, GUILayout.Width(defaultEditorLabelWidth - 24f));
            if (GUILayout.Button(gotoEffectFolderBtnContent, buttonCompact, GUILayout.Width(20f))) { SSCEditorHelper.HighlightFolderInProjectWindow(SSCSetup.effectsFolder, false, true); }
            EditorGUILayout.PropertyField(muzzleEffectsObjectProp, GUIContent.none);
            GUILayout.EndHorizontal();

            if (muzzleEffectsObjectProp.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("muzzleEffectsOffset"), muzzleFXOffsetContent);
            }
        }

        /// <summary>
        /// Draw pooling settings in the inspector
        /// </summary>
        protected void DrawPoolingSettings()
        {
            minPoolSizeProp = serializedObject.FindProperty("minPoolSize");
            maxPoolSizeProp = serializedObject.FindProperty("maxPoolSize");
            EditorGUILayout.PropertyField(minPoolSizeProp, minPoolSizeContent);
            EditorGUILayout.PropertyField(maxPoolSizeProp, maxPoolSizeContent);
            if (minPoolSizeProp.intValue > maxPoolSizeProp.intValue) { maxPoolSizeProp.intValue = minPoolSizeProp.intValue; }
        }

        /// <summary>
        /// Draw the shield effects object in the inspector
        /// </summary>
        protected void DrawShieldEffectsObject()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(shieldEffectsObjectContent, GUILayout.Width(defaultEditorLabelWidth - 24f));
            if (GUILayout.Button(gotoEffectFolderBtnContent, buttonCompact, GUILayout.Width(20f))) { SSCEditorHelper.HighlightFolderInProjectWindow(SSCSetup.effectsFolder, false, true); }
            EditorGUILayout.PropertyField(shieldEffectsObjectProp, GUIContent.none);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw the shield penetration in the inspector
        /// </summary>
        protected void DrawShieldPenetration()
        {
            EditorGUILayout.PropertyField(shieldPenetrationProp, shieldPenetrationContent);
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

            #region General Settings

            SSCEditorHelper.SSCVersionHeader(labelFieldRichText);

            EditorGUILayout.LabelField(headerContent, helpBoxRichText);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(startSpeedProp, startSpeedContent);
            EditorGUILayout.PropertyField(useGravityProp, useGravityContent);
            EditorGUILayout.PropertyField(damageTypeProp, damageTypeContent);
            EditorGUILayout.PropertyField(damageAmountProp, damageAmountContent);
            EditorGUILayout.PropertyField(isDamageSourceProp, isDamageSourceContent);
            EditorGUILayout.PropertyField(collisionLayerMaskProp, collisionLayerMaskContent);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(useECSProp, useECSContent);
            // ECS and Pooling are mutually exclusive
            if (EditorGUI.EndChangeCheck() && usePoolingProp.boolValue && useECSProp.boolValue) { usePoolingProp.boolValue = false; }
            #if !SSC_ENTITIES
            if (useECSProp.boolValue) { EditorGUILayout.HelpBox("Entity Component System not configured. Consult Help or Get Support to install the correct packages.", MessageType.Error); }
            #endif
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(usePoolingProp, usePoolingContent);
            // ECS and Pooling are mutually exclusive
            if (EditorGUI.EndChangeCheck() && usePoolingProp.boolValue && useECSProp.boolValue) { useECSProp.boolValue = false; }
            if (usePoolingProp.boolValue)
            {
                DrawPoolingSettings();
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("despawnTime"), despawnTimeContent);
            if (!useECSProp.boolValue)
            {
                EditorGUILayout.PropertyField(isKinematicGuideToTargetProp, isKinematicGuideToTargetContent);
                if (isKinematicGuideToTargetProp.boolValue)
                {
                    EditorGUILayout.PropertyField(guidedMaxTurnSpeedProp, guidedMaxTurnSpeedContent);
                }
            }

            DrawEffectsObject();
            DrawShieldEffectsObject();
            DrawShieldPenetration();
            DrawMuzzleEffectsObject();

            // Tell users not to add colliders
            if (projectileModule.GetComponentInChildren<Collider>() != null)
            {
                EditorGUILayout.HelpBox("Projectiles should not have colliders attached. Collision detection currently occurs via " +
                    "raycasting to improve performance.", MessageType.Error);
            }

            // Tell users not to add rigidbodies
            if (projectileModule.GetComponentInChildren<Rigidbody>() != null)
            {
                EditorGUILayout.HelpBox("Projectiles should not have rigidbodies attached. Position is currently updated manually to improve performance.", MessageType.Error);
            }

            EditorGUILayout.EndVertical();

            #endregion

            // Apply property changes
            serializedObject.ApplyModifiedProperties();

            #region Debug Mode
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            isDebuggingEnabled = EditorGUILayout.Toggle(debugModeContent, isDebuggingEnabled);

            if (isDebuggingEnabled && projectileModule != null)
            {
                DrawDebugBaseContent(150f);                
            }

            EditorGUILayout.EndVertical();
            #endregion
        }

        #endregion
    }
}
