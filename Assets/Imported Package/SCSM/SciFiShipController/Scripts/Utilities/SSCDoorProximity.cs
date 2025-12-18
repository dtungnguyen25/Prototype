using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    /// <summary>
    /// Component to be used with the SSCDoorAnimator component to trigger when doors are locked or unlocked.
    /// Place it on a gameobject with a single trigger collider.
    /// Either the gameobject of this component, or the item that enters the trigger area must have a rigidbody
    /// attached, otherwise the OnTriggerEnter/Exit/Stay will not be called.
    /// The rigidbody can be set to isKinematic.
    /// Optionally set the exitDelay. If the same collider enters the trigger collider before this time
    /// has expired, the exit will be cancelled and the enter action will be ignored. It will be as if
    /// the object hadn't exited from the trigger collider area.
    /// WARNING: Beware of using multiple colliders on the same gameobject as this component. This can lead
    /// to unwanted behaviour like getting Enter/Exit/Stay events when you least expect it.
    /// For example, a box non-trigger collider on this gameobject way cause a OnTriggerEnter event
    /// to fire when say a sphere trigger collider has been configured but an object with a trigger collider
    /// has entered the space of the box collider.
    /// The reason this occurs is that Unity creates a compound collider for all colliders on the same gameobject.
    /// The solution is to move the trigger collider (and this component) to a child gameobject.
    /// </summary>
    public class SSCDoorProximity : MonoBehaviour
    {
        #region Public Variables

        public SSCDoorAnimator sscDoorAnimator;

        [Tooltip("Array of zero-based door indexes from the SSCDoorAnimator to control")]
        public int[] doorIndexes;

        [Tooltip("Should the door(s) be unlocked when an object enters the collider area?")]
        public bool isUnlockDoorsOnEntry = true;

        [Tooltip("Should the door(s) be openned when an object enters the collider area? Will have no effect on locked doors.")]
        public bool isOpenDoorsOnEntry = true;

        [Tooltip("Should the door(s) be closed when an object exits the collider area? Will have no effect on locked doors.")]
        public bool isCloseDoorsOnExit = true;

        [Tooltip("Should the door(s) be locked when an object exits the collider area?")]
        public bool isLockDoorsOnExit = true;

        [Tooltip("Should there be a delay (in seconds) before the exit action is taken? If so, an object re-entering within the time will be like an object not exiting.")]
        [SerializeField, Range(0f, 5f)] private float exitDelay = 0f;

        [Tooltip("Array of Unity Tags for objects that affect this collider area. If none are provided, all objects can affect this area. NOTE: All tags MUST exist.")]
        public string[] tags = new string[] {};

        #endregion

        #region Private Variables
        private Collider proximityCollider = null;
        private bool isInitialised = false;
        private int numTags = 0;

        /// <summary>
        /// List of coroutines associated with a collider pending exiting the proxity area.
        /// Use a list rather than a HashSet to avoid having to use a Linq Where clause to find and remove them.
        /// (Linq can create a lot of garbage).
        /// </summary>
        [System.NonSerialized] private List<SSCPendingExit> pendingExitList;

        /// <summary>
        /// Unordered unique set of collider hashes pending exit.
        /// Helps to determine OnTriggerExit events
        /// </summary>
        [System.NonSerialized] private HashSet<int> exitColliders = new HashSet<int>();
        private WaitForSeconds exitWait;

        #endregion

        #region Initialisation Methods

        // Start is called before the first frame update
        void Start()
        {
            if (sscDoorAnimator != null)
            {
                // Find the first trigger collider
                Collider[] colliders = GetComponents<Collider>();

                foreach (Collider collider in colliders)
                {
                    if (collider.isTrigger)
                    {
                        proximityCollider = collider;
                        break;
                    }
                }

                if (exitDelay > 0f)
                {
                    pendingExitList = new List<SSCPendingExit>();
                    exitWait = new WaitForSeconds(exitDelay);
                }

                if (proximityCollider != null)
                {
                    ValidateTags();
                    isInitialised = true;
                }
                #if UNITY_EDITOR
                else
                {
                    Debug.LogWarning("[ERROR] SSCDoorProximity could not find a (trigger) collider component. Did you attach one to this gameobject?");
                }
                #endif
            }
            #if UNITY_EDITOR
            else
            {
                Debug.LogWarning("[ERROR] SSCDoorProximity could not find SSCDoorAnimator component for " + gameObject.name);
            }
            #endif
        }

        /// <summary>
        /// Removes any empty or null tags. NOTE: May increase GC so don't use each frame.
        /// </summary>
        private void ValidateTags()
        {
            numTags = tags == null ? 0 : tags.Length;

            if (numTags > 0)
            {
                List<string> tagList = new List<string>(tags);

                for (int tgIdx = numTags - 1; tgIdx >= 0; tgIdx--)
                {
                    // Remove invalid tag
                    if (string.IsNullOrEmpty(tagList[tgIdx])) { tagList.RemoveAt(tgIdx); }
                }

                // If there were invalid entries, update the array
                if (tagList.Count != numTags)
                {
                    tags = tagList.ToArray();
                    numTags = tags == null ? 0 : tags.Length;
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Prevent the pending exit.
        /// </summary>
        /// <param name="otherColliderHash"></param>
        private void CancelExit(int otherColliderHash)
        {
            // Find and remove the coroutine from the list
            for (int d = 0; d < pendingExitList.Count; d++)
            {
                SSCPendingExit pendingExit = pendingExitList[d];
                if (pendingExit.colliderHashCode == otherColliderHash)
                {
                    StopCoroutine(pendingExit.coroutine);
                    pendingExitList.RemoveAt(d);
                    break;
                }
            }
        }

        /// <summary>
        /// Take action on exit from the proximity area.
        /// </summary>
        /// <param name="numDoors"></param>
        private void Exit (int numDoors)
        {
            for (int dIdx = 0; dIdx < numDoors; dIdx++)
            {
                int doorNumber = doorIndexes[dIdx];

                if (isCloseDoorsOnExit)
                {
                    sscDoorAnimator.CloseDoors(doorNumber);
                }

                if (isLockDoorsOnExit)
                {
                    sscDoorAnimator.LockDoor(doorNumber);
                }
            }
        }

        /// <summary>
        /// Exit the proximity area after a short delay.
        /// </summary>
        /// <param name="otherColliderHash"></param>
        /// <param name="numDoors"></param>
        /// <returns></returns>
        private IEnumerator ExitDelay (int otherColliderHash, int numDoors)
        {
            yield return exitWait;

            exitColliders.Remove(otherColliderHash);

            CancelExit(otherColliderHash);

            Exit(numDoors);
        }

        /// <summary>
        /// Does the gameobject have a tag that matches the array configured by the user.
        /// Will return true if a match is found OR there are no tags configured.
        /// </summary>
        /// <param name="objectGameObject"></param>
        /// <returns></returns>
        private bool IsObjectTagMatched(GameObject objectGameObject)
        {
            if (!isInitialised) { return false; }

            if (objectGameObject == null) { return false; }
            else if (numTags < 1) { return true; }          
            else
            {
                bool isMatch = false;
                for (int tgIdx = 0; tgIdx < numTags; tgIdx++)
                {
                    if (objectGameObject.CompareTag(tags[tgIdx]))
                    {
                        isMatch = true;
                        break;
                    }
                }
                return isMatch;
            }
        }

        #endregion

        #region Event Methods

        private void OnTriggerEnter(Collider other)
        {
            if (isInitialised)
            {
                int numDoors = GetNumberOfDoors();

                if (numDoors > 0 && IsObjectTagMatched(other.gameObject))
                {
                    int otherColliderHash = other.GetHashCode();

                    // If we haven't finished exiting then we need to cancel any pending exits
                    if (exitDelay > 0f && exitColliders.Contains(otherColliderHash))
                    {
                        //Debug.Log("[DEBUG] Cancel exit trigger " + name + " for other " + other.name);
                        exitColliders.Remove(otherColliderHash);
                        CancelExit(otherColliderHash);
                        return;
                    }

                    //float dist = Vector3.Distance(proximityCollider.transform.position, other.transform.position);
                    //Debug.Log("[DEBUG] trigger enter " + proximityCollider.name + " other: " + other.name + " other tag: " + other.gameObject.tag + " dist: " + dist + " T:" + Time.time);

                    for (int dIdx = 0; dIdx < numDoors; dIdx++)
                    {
                        int doorNumber = doorIndexes[dIdx];

                        if (isUnlockDoorsOnEntry)
                        {
                            sscDoorAnimator.UnlockDoor(doorNumber);
                        }

                        if (isOpenDoorsOnEntry)
                        {
                            sscDoorAnimator.OpenDoors(doorNumber);
                        }
                    }
                }
            }

            //Debug.Log("[DEBUG] trigger enter " + proximityCollider.name + " other: " + other.name + " T:" + Time.time);
        }

        private void OnTriggerStay(Collider other)
        {
            if (isInitialised)
            {
                //Debug.Log("[DEBUG] trigger stay " + proximityCollider.name + " other: " + other.name + " T:" + Time.time);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (isInitialised)
            {
                int numDoors = GetNumberOfDoors();

                if (numDoors > 0 && IsObjectTagMatched(other.gameObject))
                {
                    //float dist = Vector3.Distance(proximityCollider.transform.position, other.transform.position);
                    //Debug.Log("[DEBUG] trigger exit " + proximityCollider.name + " rbName " + proximityCollider.attachedRigidbody.name + " other: " + other.name + " dist: " + dist + " T:" + Time.time);

                    if (exitDelay > 0f)
                    {
                        int colliderHash = other.GetHashCode();

                        // condition added v1.4.6 Beta 2e
                        if (!exitColliders.Contains(colliderHash))
                        {
                            exitColliders.Add(colliderHash);

                            // Start the delayed exit
                            Coroutine coroutine = StartCoroutine(ExitDelay(colliderHash, numDoors));

                            // Associating a coroutine with a collider hash...
                            pendingExitList.Add(new SSCPendingExit(coroutine, colliderHash));
                        }
                    }
                    else
                    {
                        Exit(numDoors);
                    }
                }
            }

            //Debug.Log("[DEBUG] trigger exit " + proximityCollider.name + " other: " + other.name + " T:" + Time.time);
        }

        #endregion

        #region Public API Methods

        /// <summary>
        /// Get the number of doors or sets of doors affect by this proximity component
        /// </summary>
        /// <returns></returns>
        public int GetNumberOfDoors()
        {
            return doorIndexes == null ? 0 : doorIndexes.Length;
        }

        #endregion
    }
}