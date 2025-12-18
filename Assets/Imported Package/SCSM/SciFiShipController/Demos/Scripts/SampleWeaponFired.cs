using UnityEngine;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    /// <summary>
    /// Sample script to get notification when a weapon is fired on a ship.
    /// Attach the script to your ship.
    /// This is only a code segment to demonstrate how API calls could be used in
    /// your own code. Create your own version of this in your own namespace
    /// and include a using SciFiShipController statement.
    /// </summary>
    [AddComponentMenu("Sci-Fi Ship Controller/Samples/Weapon Fired")]
    [HelpURL("https://scsmmedia.com/ssc-documentation")]
    public class SampleWeaponFired : MonoBehaviour
    {
        #region Private Variables - General
        private ShipControlModule myShipControlModule = null;
        private Ship myShip = null;
        #endregion

        #region Private Initialise Methods

        private void Start()
        {
            if (TryGetComponent(out myShipControlModule))
            {
                myShip = myShipControlModule.shipInstance;
                myShip.callbackOnWeaponFired = WeaponWasFired;
            }
        }

        #endregion

        #region Private Methods - General

        /// <summary>
        /// This is automatically called when your ship fires a weapon
        /// </summary>
        /// <param name="ship"></param>
        /// <param name="weapon"></param>
        private void WeaponWasFired(Ship ship, Weapon weapon)
        {
            // Do whatever you want here
            Debug.Log("Weapon fired " + weapon.name);
        }
        #endregion
    }
}