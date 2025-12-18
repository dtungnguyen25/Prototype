using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    /// <summary>
    /// Demo script to show how to instruct an AI Ship through the use of colliders in the scene.
    /// Also shows how to create a custom behaviour.
    /// WARNING: This is a DEMO script and is subject to change without notice during
    /// upgrades. This is just to show you how to do things in your own code and namespace.
    /// </summary>
    public class DemoLocation : MonoBehaviour
    {
        #region Public variables
        public AIBehaviourInput.AIBehaviourType primaryBehaviourType = AIBehaviourInput.AIBehaviourType.SeekArrival;
        public DemoLocation nextLocation;
        public bool showLocationLabel = false;

        [Header("Optional Squadrons to allow")]
        public int[] squadronIdFilter;

        #endregion

        #region Private variables
        private Collider locCollider;
        private int numSquadronsToFilter = 0;
        #endregion

        #region Initialisation Methods

        // Use this for initialization
        void Awake()
        {
            if (!TryGetComponent(out locCollider) || !(locCollider.GetType() == typeof(SphereCollider) || locCollider.GetType() == typeof(BoxCollider)))
            {
                #if UNITY_EDITOR
                throw new MissingComponentException("Missing Box or Sphere Collider on " + gameObject.name);
                #endif
            }
            else if(!locCollider.isTrigger)
            {
                #if UNITY_EDITOR
                throw new MissingComponentException(gameObject.name + " must be a trigger box or sphere collider");
                #endif
            }
            else
            {
                Initialise();
            }
        }
        #endregion

        #region Event Methods

        private void OnTriggerEnter(Collider other)
        {
            if (other != null && nextLocation != null)
            {
                // Is this an AI Ship?

                // Recurse upward in the hierarchy until we find the ship prefab parent
                ShipAIInputModule shipAIInputModule = other.GetComponentInParent<ShipAIInputModule>();

                if (shipAIInputModule != null)
                {
                    bool getNextLocation = numSquadronsToFilter == 0;

                    // Check if ships should only be included if in filter array
                    if (!getNextLocation)
                    {
                        int squadronId = shipAIInputModule.GetComponent<ShipControlModule>().shipInstance.squadronId;

                        for (int i = 0; i < numSquadronsToFilter; i++)
                        {
                            if (squadronId == squadronIdFilter[i]) { getNextLocation = true; break; }
                        }
                    }

                    if (getNextLocation)
                    {
                        Vector3 flytoPosition = Vector3.zero;

                        // Change the current AI behaviour type and get the next target position
                        DemoFlyToLocationShipData shipData;

                        // There should always be a DemoFlyToLocationShipData component attached.
                        if (shipAIInputModule.TryGetComponent(out shipData))
                        {
                            shipData.currentBehaviourType = primaryBehaviourType;

                            flytoPosition = nextLocation.GetPosition(shipData.offsetType, shipData.offsetPosition);
                        }
                        else
                        {
                            // Fallback just in case
                            flytoPosition = nextLocation.transform.position;
                        }

                        // Add one or more behaviours to the ShipAIModule
                        //shipAIInputModule.ClearAssignedBehaviours();
                        //shipAIInputModule.aiBehaviourList.Add(new AIBehaviour(primaryBehaviourType, 1f));


                        // Tell the AI Ship where to go
                        shipAIInputModule.AssignTargetPosition(flytoPosition);
                        

                    }
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get the world space position of this demo location give an offsetType and an offsetPosition from this demo location.
        /// </summary>
        /// <param name="offsetType"></param>
        /// <param name="offsetPosition"></param>
        /// <returns></returns>
        public Vector3 GetPosition (DemoFlyToLocation.OffsetType offsetType, Vector3 offsetPosition)
        {
            if (offsetPosition == Vector3.zero)
            {
                return transform.position;
            }
            else
            {
                if (offsetType == DemoFlyToLocation.OffsetType.LocalSpace)
                {
                    return transform.position + (transform.rotation * offsetPosition);
                }
                else if (offsetType == DemoFlyToLocation.OffsetType.WorldSpace)
                {
                    return transform.position + (Quaternion.identity * offsetPosition);
                }
                // Default to local space
                else
                {
                    return transform.position + (transform.rotation * offsetPosition);
                }
            }
        }


        /// <summary>
        /// Must be called if the squardronIdFilter array is changed
        /// </summary>
        public void Initialise()
        {
            numSquadronsToFilter = squadronIdFilter == null ? 0 : squadronIdFilter.Length;

            if (showLocationLabel)
            {
                TextMesh textMesh = gameObject.GetComponent<TextMesh>();

                if (textMesh == null) { textMesh = gameObject.AddComponent<TextMesh>(); }

                if (textMesh != null)
                {
                    textMesh.text = gameObject.name;
                    textMesh.alignment = TextAlignment.Center;
                }
            }
        }

        #endregion
    }
}