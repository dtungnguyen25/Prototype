using UnityEngine;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    /// <summary>
    /// Non-serializeable class to track temporary corountine for a pending exit
    /// from a trigger collider area.
    /// Currently used by SSCDoorProximity.
    /// </summary>
    public class SSCPendingExit
    {
        #region Public Variables

        /// <summary>
        /// The coroutine to that has started that will run the required code
        /// when to perform a delayed exit from the trigger collider.
        /// </summary>
        public Coroutine coroutine;

        /// <summary>
        /// The HashCode of the collider that is exiting from the trigger collider.
        /// </summary>
        public int colliderHashCode;
        #endregion

        #region Constructors

        public SSCPendingExit(Coroutine c, int hashCode)
        {
            coroutine = c;
            colliderHashCode = hashCode;
        }

        #endregion
    }
}