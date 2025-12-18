using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    /// <summary>
    /// This is used to keep track of items used when drawing a radar display using a texture.
    /// </summary>
    /// [Unity.VisualScripting.Inspectable]
    [HelpURL("https://scsmmedia.com/ssc-documentation")]
    public class SSCRadarDisplay
    {
        #region Public Variables

        public Texture2D radarTex;
        public Color32[] uiRimPixels;
        public Color32[] uiInnerPixels;

        public int uiTexWidth;
        public int uiTexHeight;
        public int uiTexCentreX;
        public int uiTexCentreY;
        public int uiRimWidth;

        public bool isFollowShip;
        public bool isFollowGameObject;

        public ShipControlModule shipToFollow;
        public GameObject gameobjectToFollow;
        public Quaternion uiRotation;
        public Vector3 defaultFwdDirection;

        public Color32 overlayColour;
        public Color32 backgroundColour;
        public Color32 blipFriendColour;
        public Color32 blipFoeColour;
        public Color32 blipNeutralColour;

        #endregion

        #region Public Properties

        #endregion

        #region Public Static Variables

        #endregion

        #region Private Variables - General

        #endregion

        #region Constructors

        public SSCRadarDisplay()
        {
            SetClassDefaults();
        }

        #endregion


        #region Private and Internal Methods - General

        #endregion

        #region Public API Methods - General

        public void SetClassDefaults()
        {
            radarTex = null;
            uiRimPixels = null;
            uiInnerPixels = null;

            uiTexWidth = 10;
            uiTexHeight = 10;
            uiTexCentreX = 5;
            uiTexCentreY = 5;
            uiRimWidth = 4;

            isFollowShip = false;
            isFollowGameObject = false;
            shipToFollow = null;
            gameobjectToFollow = null;
            defaultFwdDirection = Vector3.forward;

            overlayColour = Color.blue;
            backgroundColour = Color.black;
            blipFriendColour = Color.green;
            blipFoeColour = Color.red;
            blipNeutralColour = Color.white;
        }

        #endregion

    }
}