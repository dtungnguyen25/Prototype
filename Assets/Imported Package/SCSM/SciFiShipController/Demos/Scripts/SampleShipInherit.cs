using UnityEngine;
using SciFiShipController;

// Sci-Fi Ship Controller. Copyright (c) 2018-2025 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipControllerSample
{
    /// <summary>
    /// Sample script to inherit from ShipControlModule, override rigidbody force and update cached move data.
    /// This could be used if you have a custom rigidbody like for a multiplayer game.
    /// See also SampleShipInheritEditor
    /// </summary>
    [HelpURL("https://scsmmedia.com/ssc-documentation")]
    public class SampleShipInherit : ShipControlModule
    {
        #region Public Variables

        // Add a reference to your custom rigidbody here


        [Range(0f,10f)] public float mySampleVariable = 0;

        #endregion

        #region Override Methods

        protected override void ApplyForceAndMoment()
        {
            // Replace this with applying force to your custom rigidbody
            // using localResultantForce and localResultantMoment

            base.ApplyForceAndMoment();
        }

        /// <summary>
        /// Unsubscribe from callbackOnMoveDataUpdate. 
        /// </summary>
        protected override void OnDestroyGameObject()
        {
            if (IsInitialised && shipInstance != null)
            {
                shipInstance.callbackOnMoveDataUpdate -= OnMoveDataUpdate;
            }

            base.OnDestroyGameObject();
        }

        public override void InitialiseShip()
        {
            base.InitialiseShip();

            if (IsInitialised && shipInstance != null && shipInstance.moveDataUpdateInt == Ship.SSCMoveDataCustomInt)
            {
                // Subscribe to get notified when cached data needs to be updated
                shipInstance.callbackOnMoveDataUpdate += OnMoveDataUpdate; 
            }
        }

        #endregion

        #region Callback Method

        /// <summary>
        /// This is automatically called by SSC when data needs to be refreshed.
        /// </summary>
        /// <param name="ship"></param>
        /// <param name="tfrm"></param>
        /// <param name="rigidbody"></param>
        private void OnMoveDataUpdate(Ship ship, Transform tfrm, Rigidbody rigidbody)
        {
            // Modify this to provide data from your custom rigidbody setup

            ship.SetTransformData(tfrm.position, tfrm.rotation);

            #if UNITY_6000_0_OR_NEWER
            Vector3 linearVelocity = rigidbody.linearVelocity;
            #else
            Vector3 linearVelocity = rigidbody.velocity;
            #endif

            Vector3 angularVelocity = rigidbody.angularVelocity;
            Vector3 rBodyInertiaTensor = rigidbody.inertiaTensor;

            ship.SetRigidBodyData(rigidbody.position, rigidbody.rotation, linearVelocity, angularVelocity, rBodyInertiaTensor);
        }

        #endregion
    }
}