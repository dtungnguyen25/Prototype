using System.Collections.Generic;
using UnityEngine;

// Sci-Fi Ship Controller. Copyright (c) 2018-2025 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    /// <summary>
    /// A scriptableobject used to store common gauge purposes used in a ShipDisplayModule.
    /// </summary>
    [CreateAssetMenu(fileName = "SSC Gauge Purposes", menuName = "Sci-Fi Ship Controller/Gauge Purposes")]
    [HelpURL("https://scsmmedia.com/ssc-documentation")]
    public class SSCGaugePurposes : SSCLookup
    {
        #region Public Variables

        #endregion

        #region Public Properties

        public override string ElementPrefix => "Purpose";
        public override string LookupsShortName => "Custom Purposes";

        #endregion


        #region Public API Methods

        #endregion
    }
}