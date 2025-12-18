using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    /// <summary>
    /// Class used for storing AI Data on a ship to be used by the AI state method.
    /// </summary>
    public class DemoFlyToLocationShipData : MonoBehaviour
    {
        public AIBehaviourInput.AIBehaviourType currentBehaviourType = AIBehaviourInput.AIBehaviourType.CustomSeekArrival;


        public DemoFlyToLocation.OffsetType offsetType = DemoFlyToLocation.OffsetType.LocalSpace;

        /// <summary>
        /// Fly to a demo location with a offset.
        /// </summary>
        public Vector3 offsetPosition = Vector3.zero;
    }
}
