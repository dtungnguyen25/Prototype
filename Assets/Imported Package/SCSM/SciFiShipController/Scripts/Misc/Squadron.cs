using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    [System.Serializable]
    public class Squadron
    {
        #region Enumerations

        /// <summary>
        /// The Tactical shape of the formation
        /// </summary>
        public enum TacticalFormation
        {
            Vic = 0,
            Wedge = 1,
            Line = 2,
            Column = 3,
            StaggeredColumn = 4,
            LeftEchelon = 5,
            RightEchelon = 6
        }

        #endregion

        #region Public variables

        // IMPORTANT - when changing this section also update SetClassDefault()
        // Also update ClassName(ClassName className) Clone Constructor (if there is one)

        /// <summary>
        /// By default the squadronId is set to -1 to denote that it hasn't been set.
        /// We "could" give it some hashed name to begin with in an attempt to create
        /// unique names. We don't want to create a GUID because strings aren't useable in DOTS.
        /// </summary>
        public int squadronId;
        public string squadronName;

        // To be converted to int for DOTS
        public TacticalFormation tacticalFormation;

        /// <summary>
        /// The prefab of the ship that will be instantiated
        /// for squadron members. See also ShipSpawner.cs
        /// </summary>
        public GameObject shipPrefab;

        /// <summary>
        /// An optional reference to a player ship from the scene that can be placed
        /// within a squadron. The player must already exist. If it doesn't,
        /// a shipPrefab will be instantiated in its place.
        /// See also ShipSpawner.cs
        /// </summary>
        public GameObject playerShip;

        /// <summary>
        /// The default TargetOffset for a camera following the ship
        /// </summary>
        public Vector3 cameraTargetOffset;

        /// <summary>
        /// The faction or alliance the squadron belongs to. This can be used to identify
        /// if a squadron is friend or foe.
        /// </summary>
        public int factionId;

        /// <summary>
        /// The current members of this squadron. Stores the
        /// shipId for each member which is a session-only
        /// transform InstanceID.
        /// </summary>
        [System.NonSerialized] public List<int> shipList;

        /// <summary>
        /// The current members of this squadron.
        /// </summary>
        [System.NonSerialized] public List<ShipControlModule> shipControlModuleList;

        /// <summary>
        /// Central "front" position of the squadron in worldspace
        /// </summary>
        public Vector3 anchorPosition;

        /// <summary>
        /// The direction the squadron is facing
        /// </summary>
        public Vector3 fwdDirection;

        /// <summary>
        /// The distance apart (centre to centre) ships should be spaced on the x-axis
        /// </summary>
        public float offsetX;

        /// <summary>
        /// The distance apart (centre to centre) ships should be spaced on the y-axis (up)
        /// </summary>
        public float offsetY;

        /// <summary>
        /// The distance apart (centre to centre) ships should be spaced on the z-axis
        /// </summary>
        public float offsetZ;

        /// <summary>
        /// (Max) Number of rows of ships on the x-axis. This will depend on the tactical formation type
        /// </summary>
        public int rowsX;

        /// <summary>
        /// (Max) Number of rows of ships on the y-axis. This will depend on the tactical formation type
        /// This is used for 3-dimensional formations. Default is 1.
        /// </summary>
        public int rowsY;

        /// <summary>
        /// (Max) Number of rows of ships on the z-axis. This will depend on the tactical formation type
        /// </summary>
        public int rowsZ;

        /// <summary>
        /// Whether the squadron is shown as expanded in the inspector window of the editor.
        /// </summary>
        public bool showInEditor;

        #endregion

        #region Public Properties

        /// <summary>
        /// Get the number of ships currently allocated to this squadron.
        /// NOTE: If RemoveShip(..) has not been called before a ship was removed
        /// from the scene, this number may be incorrect.
        /// </summary>
        public int NumberOfShips { get { return shipControlModuleList.Count; } }

        #endregion


        #region Private variables

        #endregion

        #region Constructors

        // Class constructor
        public Squadron()
        {
            SetClassDefaults();
        }

        // Copy constructor
        public Squadron(Squadron squadron)
        {
            if (squadron == null) { SetClassDefaults(); }
            else
            {
                squadronId = squadron.squadronId;
                squadronName = squadron.squadronName;
                tacticalFormation = squadron.tacticalFormation;
                shipPrefab = squadron.shipPrefab;
                playerShip = squadron.playerShip;
                cameraTargetOffset = squadron.cameraTargetOffset;

                // As this is a list of value types, don't need to do a deep copy
                if (squadron.shipList == null) { shipList = new List<int>(5); }
                else { shipList = new List<int>(squadron.shipList); }

                // ShipControlModule doesn't support deep copy with CovertAll...
                if (squadron.shipControlModuleList == null) { shipControlModuleList = new List<ShipControlModule>(5); }
                else { shipControlModuleList.AddRange(squadron.shipControlModuleList); }

                anchorPosition = squadron.anchorPosition;
                fwdDirection = squadron.fwdDirection;
                offsetX = squadron.offsetX;
                offsetY = squadron.offsetY;
                offsetZ = squadron.offsetZ;

                rowsX = squadron.rowsX;
                rowsY = squadron.rowsY;
                rowsZ = squadron.rowsZ;

                factionId = squadron.factionId;
                showInEditor = squadron.showInEditor;
            }
        }

        #endregion

        #region Private Methods

        private void SetClassDefaults()
        {
            squadronId = -1; // NOT SET
            squadronName = "no name";
            tacticalFormation = TacticalFormation.Vic;
            shipPrefab = null;
            playerShip = null;
            cameraTargetOffset = new Vector3(0f, 2f, -10f);
            shipList = new List<int>(5);
            shipControlModuleList = new List<ShipControlModule>(5);
            anchorPosition = Vector3.zero;
            fwdDirection = Vector3.forward;
            offsetX = 50f;
            offsetY = 100f;
            offsetZ = 50f;
            rowsX = 3;
            rowsY = 1;
            rowsZ = 3;

            // By default, all Squadrons on the same faction/side/alliance.
            factionId = 0;

            showInEditor = true;
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Attempt to get a ship from the squadron using the ShipId.
        /// </summary>
        /// <param name="squadron"></param>
        /// <param name="shipId"></param>
        /// <returns></returns>
        public static ShipControlModule GetShip (Squadron squadron, int shipId)
        {
            ShipControlModule shipControlModule = null;

            if (squadron != null)
            {
                int numShips = squadron.shipControlModuleList.Count;

                for (int sIdx = 0; sIdx < numShips; sIdx++)
                {
                    ShipControlModule _shipControlModule = squadron.shipControlModuleList[sIdx];

                    if (_shipControlModule != null && _shipControlModule.GetShipId == shipId)
                    {
                        shipControlModule = _shipControlModule;
                        break;
                    }
                }
            }

            return shipControlModule;
        }

        /// <summary>
        /// Given a list of squadrons, get the squadron with the matching Id (if any).
        /// </summary>
        /// <param name="squadronList"></param>
        /// <param name="squadronId"></param>
        /// <param name="squadron"></param>
        /// <returns>True if a squadron is found with the matching squadronId</returns>
        public static bool GetSquadron (List<Squadron> squadronList, int squadronId, out Squadron squadron)
        {
            bool isFound = false;
            squadron = null;

            int numSquadrons = squadronList == null ? 0 : squadronList.Count;

            if (numSquadrons == 1)
            {
                squadron = squadronList[0];
                isFound = squadron != null;
            }
            else if (numSquadrons > 1)
            {
                for (int sIdx = 0; sIdx < numSquadrons; sIdx++)
                {
                    Squadron _squadron = squadronList[sIdx];

                    if (_squadron != null && _squadron.squadronId == squadronId)
                    {
                        squadron = _squadron;
                        isFound = true;
                        break;
                    }
                }
            }

            return isFound;
        }

        /// <summary>
        /// Perform a fast lookup to get a squadron given a ShipId. This does not guarantee a ship still exists
        /// in the list but it is a good indicator.
        /// </summary>
        /// <param name="squadronList"></param>
        /// <param name="shipId"></param>
        /// <param name="squadron"></param>
        /// <returns></returns>
        public static bool GetSquadronWithShipFast (List<Squadron> squadronList, int shipId, out Squadron squadron)
        {
            bool isFound = false;

            squadron = null;

            int numSquadrons = squadronList == null ? 0 : squadronList.Count;

            for (int sIdx = 0; sIdx < numSquadrons; sIdx++)
            {
                Squadron _squadron = squadronList[sIdx];

                if (_squadron != null && _squadron.shipList.Contains(shipId))
                {
                    squadron = _squadron;
                    isFound = true;
                    break;
                }
            }

            return isFound;
        }

        /// <summary>
        /// Given a ship Id, attempt to remove a ship from the squadron.
        /// </summary>
        /// <param name="squadron"></param>
        /// <param name="shipId"></param>
        public static void RemoveShip (Squadron squadron, int shipId)
        {
            if (squadron != null)
            {
                int numShips = squadron.shipControlModuleList.Count;

                for (int sIdx = 0; sIdx < numShips; sIdx++)
                {
                    ShipControlModule _shipControlModule = squadron.shipControlModuleList[sIdx];

                    if (_shipControlModule != null && _shipControlModule.GetShipId == shipId)
                    {
                        squadron.shipControlModuleList.RemoveAt(sIdx);
                        break;
                    }
                }

                squadron.shipList.Remove(shipId);
            }
        }

        #endregion
    }
}