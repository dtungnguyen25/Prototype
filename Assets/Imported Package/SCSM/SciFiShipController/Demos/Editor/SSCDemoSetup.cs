using UnityEditor;
using UnityEngine;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    // Script that performs miscellaneous setup tasks for Sci-Fi Ship Controller demos - runs when the project is opened in the Unity editor
    // See also SSCSetup

    [InitializeOnLoad]
    public static class SSCDemoSetup
    {
        #region Constructor
        static SSCDemoSetup()
        {
            // For SSCCelstials layer, see SSCSetup.cs
            int[] layerNumbersToAdd = { 27 };
            //int[] layerNumbersToAdd = { Celestials.celestialsUnityLayer, 27 };
            //string[] layersToAdd = { "SSC Celestials", "Small Ships" };
            string[] layersToAdd = { "Small Ships" };
            string[] tagsToAdd = { "NPC" };

            SSCSetup.FindTagAndLayerManager();
            SSCSetup.CreateLayers(layersToAdd, layerNumbersToAdd);
            SSCSetup.CreateTags(tagsToAdd);

            // If not found, attempt to add a NonClippable layer used in Tech Demo 4
            // for camera object clipping
            if (LayerMask.NameToLayer("NonClippable") < 0)
            {
                SSCSetup.CreateLayers(new string[] { "NonClippable" }, new int[] { 24 });
            }
        }

        #endregion
    }
}