using System;
using UnityEditor;
using UnityEngine;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    /// <summary>
    /// The custom drawer for the SSCSeatStage in the Inspector
    /// </summary>
    [CustomPropertyDrawer(typeof(SSCSeatStage))]
    public class SSCSeatStageDrawer : PropertyDrawer
    {
        #region GUIContent
        protected readonly static GUIContent stageNameContent = new GUIContent("Stage Name", "The descriptive name of the Seat Stage");
        protected readonly static GUIContent activateSpeedContent = new GUIContent("Activate Speed", "The speed at which the activate animation runs");
        protected readonly static GUIContent deactivateSpeedContent = new GUIContent("Deactivate Speed", "The speed at which the deactivate animation runs");
        protected readonly static GUIContent maxAudioVolumeContent = new GUIContent("Max Audio Volume", "The maximum volume of the audio clip relative to the initial AudioSource volume");
        protected readonly static GUIContent audioClipContent = new GUIContent("Audio Clip", "The audio clip that is played when the stage is played");
        protected readonly static GUIContent paramBoolValueContent = new GUIContent("Bool Param Value", "The expected value of the bool parameter in the Animator Controller when the stage runs");
        protected readonly static GUIContent parameterTypeContent = new GUIContent("Parameter Type", "The type of parameter in the Animator Controller");
        protected readonly static GUIContent activateParamNameContent = new GUIContent("Activate Parameter", "The name of the parameter in the Animator Controller");
        protected readonly static GUIContent deactivateParamNameContent = new GUIContent("Deactivate Parameter", "The name of the trigger parameter in the Animator Controller");
        protected readonly static GUIContent completionTypeContent = new GUIContent("Completion Type", "How to determine when the stage has been completed");
        protected readonly static GUIContent completionActionContent = new GUIContent("Completion Action", "What do to when the stage is completed");
        protected readonly static GUIContent stageDurationContent = new GUIContent("Stage Duration", "How many seconds the stage should take to complete when completion type is ByDuration.");
        protected readonly static GUIContent isAlwaysCallPreStageContent = new GUIContent("Always call PreStage", "If the stage is already in the final expected state, should the onPreStage methods always be called.");
        protected readonly static GUIContent isAlwaysCallPostStageContent = new GUIContent("Always call PostStage", "If the stage is already in the final expected state, should the onPostStage methods always be called.");
        protected readonly static GUIContent onPreStageContent = new GUIContent("On PreStage", "");
        protected readonly static GUIContent onPostStageContent = new GUIContent("On PostStage", "");
        #endregion

        #region Static Variables
        protected readonly static float lineSpacing = 3f;
        protected readonly static float lineHeight = EditorGUIUtility.singleLineHeight;
        #endregion

        #region Protected Methods
       
        protected void DrawStageProperty(SerializedProperty property, GUIContent labelContent, float totalWidth, ref float xPos, ref float yPos, float labelWidth, float fieldWidth)
        {
            //EditorGUI.PrefixLabel(new Rect(xPos, yPos, labelWidth, lineHeight), GUIUtility.GetControlID(FocusType.Passive), labelContent);
            EditorGUI.LabelField(new Rect(xPos, yPos, labelWidth, lineHeight), labelContent);
            EditorGUI.PropertyField(new Rect(xPos + labelWidth + 3f, yPos, totalWidth - labelWidth - 3f, lineHeight), property, GUIContent.none);

            xPos += labelWidth;
            //EditorGUI.PropertyField(new Rect(xPos, yPos, totalWidth, lineHeight), property, labelContent);
        }


        protected void DrawStageEventProperty(SerializedProperty property, GUIContent labelContent, float totalWidth, float xPos, ref float yPos)
        {
            EditorGUI.PropertyField(new Rect(xPos, yPos, totalWidth, lineHeight), property, labelContent);
        }

        protected void DrawStageSlider(SerializedProperty property, GUIContent labelContent, float totalWidth, float min, float max, ref float xPos, ref float yPos, float labelWidth, float fieldWidth)
        {
            if (property.propertyType == SerializedPropertyType.Float)
            {
                EditorGUI.Slider(new Rect(xPos, yPos, totalWidth, lineHeight), property, min, max, labelContent);
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                EditorGUI.IntSlider(new Rect(xPos, yPos, totalWidth, lineHeight), property, Convert.ToInt32(min), Convert.ToInt32(max), labelContent);
            }

            xPos += totalWidth;
        }

        #endregion

        #region Public Methods

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            //return EditorGUI.GetPropertyHeight(property);

            int lineCount = 0;
            float totalHeight = 0f;

            if (!property.FindPropertyRelative("showInEditor").boolValue)
            {
                lineCount = 1;
                totalHeight = lineHeight * lineCount + (EditorGUIUtility.standardVerticalSpacing + lineSpacing) * (lineCount - 1);
            }
            else
            {
                lineCount = 12;

                if (property.FindPropertyRelative("completionType").intValue == SSCSeatStage.CompletionTypeIntByDuration)
                {
                    lineCount += 1;
                }

                // Get the (variable) heights of the event properties
                SerializedProperty onPreStageProp = property.FindPropertyRelative("onPreStage");
                SerializedProperty onPostStageProp = property.FindPropertyRelative("onPostStage");

                float preStageHeight = EditorGUI.GetPropertyHeight(onPreStageProp) + lineSpacing;
                float postStageHeight = EditorGUI.GetPropertyHeight(onPostStageProp) + lineSpacing;

                totalHeight = lineHeight * lineCount + (EditorGUIUtility.standardVerticalSpacing + lineSpacing) * (lineCount - 1);

                totalHeight += preStageHeight;
                totalHeight += postStageHeight;
            }
            return totalHeight;
        }

        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            #region Find properties
            SerializedProperty activateSpeedProp = property.FindPropertyRelative("activateSpeed");
            SerializedProperty deactivateSpeedProp = property.FindPropertyRelative("deactivateSpeed");
            SerializedProperty maxAudioVolumeProp = property.FindPropertyRelative("maxAudioVolume");
            SerializedProperty audioClipProp = property.FindPropertyRelative("audioClip");
            SerializedProperty paramBoolValueProp = property.FindPropertyRelative("paramBoolValue");
            SerializedProperty showInEditorProp = property.FindPropertyRelative("showInEditor");
            SerializedProperty stageNameProp = property.FindPropertyRelative("stageName");
            SerializedProperty parameterTypeProp = property.FindPropertyRelative("parameterType");
            SerializedProperty activateParamNameProp = property.FindPropertyRelative("activateParamName");
            SerializedProperty deactivateParamNameProp = property.FindPropertyRelative("deactivateParamName");
            SerializedProperty completionTypeProp = property.FindPropertyRelative("completionType");
            SerializedProperty stageDurationProp = property.FindPropertyRelative("stageDuration");
            SerializedProperty completionActionProp = property.FindPropertyRelative("completionAction");
            SerializedProperty isAlwaysCallPreStageProp = property.FindPropertyRelative("isAlwaysCallPreStage");
            SerializedProperty isAlwaysCallPostStageProp = property.FindPropertyRelative("isAlwaysCallPostStage");
            SerializedProperty onPreStageProp = property.FindPropertyRelative("onPreStage");
            SerializedProperty onPostStageProp = property.FindPropertyRelative("onPostStage");
            #endregion

            float labelWidth = EditorGUIUtility.labelWidth;
            float fieldWidth = EditorGUIUtility.fieldWidth;

            float xIndent = 0f;
            float yPos = position.y, xPos = position.x + xIndent, width = labelWidth;
            float remainingWidth = position.width - width;

            // Always show stage name
            showInEditorProp.boolValue = EditorGUI.Foldout(new Rect(xPos + 10f, yPos, 15f, lineHeight), showInEditorProp.boolValue, GUIContent.none);
            EditorGUI.LabelField(new Rect(xPos + 20f, yPos, labelWidth - 30f, lineHeight), stageNameContent);
            EditorGUI.PropertyField(new Rect(xPos + labelWidth + 3f, yPos, position.width - labelWidth - 3f, lineHeight), stageNameProp, GUIContent.none);

            if (showInEditorProp.boolValue)
            {

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawStageProperty(parameterTypeProp, parameterTypeContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawStageProperty(activateParamNameProp, activateParamNameContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);

                if (parameterTypeProp.intValue == SSCSeatAnimator.ParamTypeIntTrigger)
                {
                    xPos = position.x + xIndent;
                    yPos += lineHeight + lineSpacing;
                    DrawStageProperty(deactivateParamNameProp, deactivateParamNameContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);
                }
                else
                {
                    xPos = position.x + xIndent;
                    yPos += lineHeight + lineSpacing;
                    DrawStageProperty(paramBoolValueProp, paramBoolValueContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);
                }

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawStageSlider(activateSpeedProp, activateSpeedContent, position.width, 0.01f, 10f, ref xPos, ref yPos, labelWidth, fieldWidth);

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawStageSlider(deactivateSpeedProp, deactivateSpeedContent, position.width, 0.01f, 10f, ref xPos, ref yPos, labelWidth, fieldWidth);

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawStageProperty(completionTypeProp, completionTypeContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);

                if (completionTypeProp.intValue == SSCSeatStage.CompletionTypeIntByDuration)
                {
                    xPos = position.x + xIndent;
                    yPos += lineHeight + lineSpacing;
                    DrawStageProperty(stageDurationProp, stageDurationContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);
                }

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawStageProperty(completionActionProp, completionActionContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawStageProperty(audioClipProp, audioClipContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawStageSlider(maxAudioVolumeProp, maxAudioVolumeContent, position.width, 0f, 1f, ref xPos, ref yPos, labelWidth, fieldWidth);

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawStageProperty(isAlwaysCallPreStageProp, isAlwaysCallPreStageContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawStageProperty(isAlwaysCallPostStageProp, isAlwaysCallPostStageContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawStageEventProperty(onPreStageProp, onPreStageContent, position.width, xPos, ref yPos);

                xPos = position.x + xIndent;
                yPos += EditorGUI.GetPropertyHeight(onPreStageProp) + lineSpacing;
                DrawStageEventProperty(onPostStageProp, onPostStageContent, position.width, xPos, ref yPos);
            }

            EditorGUI.EndProperty();
        }

        #endregion
    }
}