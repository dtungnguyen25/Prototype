using System;
using UnityEditor;
using UnityEngine;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    /// <summary>
    /// The custom drawer for the SSCSeatStage in the Inspector
    /// </summary>
    [CustomPropertyDrawer(typeof(Squadron))]
    public class SquadronDrawer : PropertyDrawer
    {
        #region GUIContent - Squadrons
        protected readonly static GUIContent squadronIdContent = new GUIContent("Squadron Id", "The unique number or ID for this squadron");
        protected readonly static GUIContent squadronNameContent = new GUIContent("Squadron Name");
        protected readonly static GUIContent factionIdContent = new GUIContent("Faction Id", "The Faction that the squadron belongs to or fights for.");
        protected readonly static GUIContent anchorPositionContent = new GUIContent("Anchor Position", "The initial front middle position of the squadron. If there is more than one row on y-axis, rows will be created above this position.");
        protected readonly static GUIContent fwdDirectionContent = new GUIContent("Forward Direction", "Direction as a normalised vector");
        protected readonly static GUIContent anchorRotationContent = new GUIContent("Anchor Rotation", "The forward direction as euler angles. This is modified by setting the Forward Direction vector");
        protected readonly static GUIContent tacticalFormationContent = new GUIContent("Tactical Formation", "The type of formation in which to spawn ships");
        protected readonly static GUIContent rowsXContent = new GUIContent("Rows on x-axis", "The number of rows along the x-axis");
        protected readonly static GUIContent rowsZContent = new GUIContent("Rows on z-axis", "The number of rows along the z-axis");
        protected readonly static GUIContent rowsYContent = new GUIContent("Rows on y-axis", "The number of rows along the y-axis");
        protected readonly static GUIContent offsetXContent = new GUIContent("Row offset x-axis", "The distance between rows on the x-axis");
        protected readonly static GUIContent offsetZContent = new GUIContent("Row offset z-axis", "The distance between rows on the z-axis");
        protected readonly static GUIContent offsetYContent = new GUIContent("Row offset y-axis", "The distance between rows on the y-axis");
        protected readonly static GUIContent shipPrefabContent = new GUIContent("NPC Ship Prefab", "Non-Player-Character ship which will be used to populate the squadron");
        protected readonly static GUIContent playerShipContent = new GUIContent("Player Ship", "Optionally reference to a player ship in the scene to lead this squadron");
        protected readonly static GUIContent cameraTargetOffsetContent = new GUIContent("Camera Ship Offset", "The offset from the ship (in local space) for the camera to aim for.");
        #endregion

        #region Static Variables
        protected readonly static float lineSpacing = 3f;
        protected readonly static float lineHeight = EditorGUIUtility.singleLineHeight;
        #endregion

        #region Protected Methods

        protected void DrawSquadronProperty(SerializedProperty property, GUIContent labelContent, float totalWidth, ref float xPos, ref float yPos, float labelWidth, float fieldWidth)
        {
            EditorGUI.LabelField(new Rect(xPos, yPos, labelWidth, lineHeight), labelContent);
            EditorGUI.PropertyField(new Rect(xPos + labelWidth + 3f, yPos, totalWidth - labelWidth - 3f, lineHeight), property, GUIContent.none);

            xPos += labelWidth;
        }

        protected void DrawSquadronSlider(SerializedProperty property, GUIContent labelContent, float totalWidth, float min, float max, ref float xPos, ref float yPos, float labelWidth, float fieldWidth)
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
                lineCount = 15;

                if (property.FindPropertyRelative("tacticalFormation").intValue != (int)Squadron.TacticalFormation.Vic && property.FindPropertyRelative("tacticalFormation").intValue != (int)Squadron.TacticalFormation.Wedge)
                {
                    lineCount++;
                }


                totalHeight = lineHeight * lineCount + (EditorGUIUtility.standardVerticalSpacing + lineSpacing) * (lineCount - 1);
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

            SerializedProperty showInEditorProp = property.FindPropertyRelative("showInEditor");
            SerializedProperty squadronNameProp = property.FindPropertyRelative("squadronName");
            SerializedProperty squadronIdProp = property.FindPropertyRelative("squadronId");
            SerializedProperty factionIdProp = property.FindPropertyRelative("factionId");
            SerializedProperty anchorPositionProp = property.FindPropertyRelative("anchorPosition");
            SerializedProperty tacticalFormationProp = property.FindPropertyRelative("tacticalFormation");
            SerializedProperty fwdDirectionProp = property.FindPropertyRelative("fwdDirection");
            SerializedProperty rowsXProp = property.FindPropertyRelative("rowsX");
            SerializedProperty rowsZProp = property.FindPropertyRelative("rowsZ");
            SerializedProperty rowsYProp = property.FindPropertyRelative("rowsY");
            SerializedProperty offsetXProp = property.FindPropertyRelative("offsetX");
            SerializedProperty offsetZProp = property.FindPropertyRelative("offsetZ");
            SerializedProperty offsetYProp = property.FindPropertyRelative("offsetY");
            SerializedProperty shipPrefabProp = property.FindPropertyRelative("shipPrefab");
            SerializedProperty playerShipProp = property.FindPropertyRelative("playerShip");
            SerializedProperty cameraTargetOffsetProp = property.FindPropertyRelative("cameraTargetOffset");

            #endregion

            float labelWidth = EditorGUIUtility.labelWidth;
            float fieldWidth = EditorGUIUtility.fieldWidth;

            float xIndent = 0f;
            float yPos = position.y, xPos = position.x + xIndent, width = labelWidth;
            float remainingWidth = position.width - width;

            // Prevent zero rotation
            if (fwdDirectionProp.vector3Value == Vector3.zero) { fwdDirectionProp.vector3Value = Vector3.forward; }

            // Always show squadron name
            string nameTxt = string.IsNullOrEmpty(squadronNameProp.stringValue) ? "No Name" : squadronNameProp.stringValue + " (Id: " + squadronIdProp.intValue + ")";
            showInEditorProp.boolValue = EditorGUI.Foldout(new Rect(xPos + 10f, yPos, 15f, lineHeight), showInEditorProp.boolValue, GUIContent.none);
            EditorGUI.LabelField(new Rect(xPos + 20f, yPos, position.width, lineHeight), nameTxt);

            if (showInEditorProp.boolValue)
            {
                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawSquadronProperty(squadronIdProp, squadronIdContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawSquadronProperty(squadronNameProp, squadronNameContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawSquadronProperty(factionIdProp, factionIdContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawSquadronProperty(anchorPositionProp, anchorPositionContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);

                // Show the forward direction as a non-editable rotation
                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                GUI.enabled = false;
                EditorGUI.LabelField(new Rect(xPos, yPos, labelWidth, lineHeight), anchorRotationContent);
                EditorGUI.Vector3Field(new Rect(xPos + labelWidth + 3f, yPos, position.width - labelWidth - 3f, lineHeight), GUIContent.none, Quaternion.LookRotation(fwdDirectionProp.vector3Value, Vector3.up).eulerAngles);
                GUI.enabled = true;

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawSquadronProperty(fwdDirectionProp, fwdDirectionContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawSquadronProperty(tacticalFormationProp, tacticalFormationContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);


                if (tacticalFormationProp.intValue != (int)Squadron.TacticalFormation.Vic && tacticalFormationProp.intValue != (int)Squadron.TacticalFormation.Wedge)
                {
                    xPos = position.x + xIndent;
                    yPos += lineHeight + lineSpacing;
                    DrawSquadronProperty(rowsXProp, rowsXContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);
                }


                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawSquadronProperty(rowsZProp, rowsZContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawSquadronProperty(rowsYProp, rowsYContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawSquadronProperty(offsetXProp, offsetXContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawSquadronProperty(offsetZProp, offsetZContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawSquadronProperty(offsetYProp, offsetYContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawSquadronProperty(shipPrefabProp, shipPrefabContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawSquadronProperty(playerShipProp, playerShipContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);

                xPos = position.x + xIndent;
                yPos += lineHeight + lineSpacing;
                DrawSquadronProperty(cameraTargetOffsetProp, cameraTargetOffsetContent, position.width, ref xPos, ref yPos, labelWidth, fieldWidth);


                //xPos = position.x + xIndent;
                //yPos += lineHeight + lineSpacing;
                //DrawSquadronSlider(activateSpeedProp, activateSpeedContent, position.width, 0.01f, 10f, ref xPos, ref yPos, labelWidth, fieldWidth);

            }

            EditorGUI.EndProperty();
        }

        #endregion
    }
}