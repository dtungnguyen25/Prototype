using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    /// <summary>
    /// This component enables you to perform a multi-stage animation to 
    /// rotate and move a seat. Tyically used for cockpit seating. You may need
    /// to perform external operations during the stages using the events.
    /// </summary>
    [HelpURL("https://scsmmedia.com/ssc-documentation")]
    [RequireComponent(typeof(Animator))]
    public class SSCSeatAnimator : MonoBehaviour
    {

        #region Enumerations
        public enum ParameterType
        {
            Bool = 0,
            Trigger = 1
        }

        #endregion

        #region Public Variables


        public bool initialiseOnAwake = false;

        [HideInInspector] public bool allowRepaint = false;

        #endregion

        #region Public Properties

        public string ActivateStartStageName { get { return activateStartStageName; } set { SetActivateStartStage(value); } }

        public string DeactivateStartStageName { get { return deactivateStartStageName; } set { SetDeactivateStartStage(value); } }

        /// <summary>
        /// Is the seat considered to be fully activated?
        /// </summary>
        public bool IsActivated { get { return isActivated; } }

        /// <summary>
        /// Is activation in progess?
        /// </summary>
        public bool IsActivating { get { return isActivating; } }

        /// <summary>
        /// Is deactivation in progress?
        /// </summary>
        public bool IsDeactivating { get { return isDeactivating; } }

        public bool IsInitialised { get { return isInitialised; } }

        public int NumberOfStages { get { return numStages; } }

        public List<SSCSeatStage> SeatStageList { get { return seatStageList; } }

        #endregion

        #region Public Static Variables

        public static readonly int ParamTypeIntBool = (int)ParameterType.Bool;
        public static readonly int ParamTypeIntTrigger = (int)ParameterType.Trigger;


        #endregion

        #region Protected Variables - Serialized

        [SerializeField] protected List<SSCSeatStage> seatStageList = new List<SSCSeatStage>();

        /// <summary>
        /// The name of the stage that is played first when Activate() is called.
        /// </summary>
        [SerializeField] protected string activateStartStageName;

        /// <summary>
        /// The name of the stage that, when completed, will mark the seat as activated.
        /// </summary>
        [SerializeField] protected string activateEndStageName;

        /// <summary>
        /// The name of the stage that is played first when Deactivate() is called.
        /// </summary>
        [SerializeField] protected string deactivateStartStageName;

        /// <summary>
        /// The name of the stage that, when completed, will mark the seat as deactivated.
        /// </summary>
        [SerializeField] protected string deactivateEndStageName;


        #endregion

        #region Protected Variables - General

        /// <summary>
        /// The stage that will be played when Activate() is called.
        /// </summary>
        protected SSCSeatStage activateStartStage = null;

        protected SSCSeatStage activateEndStage = null;

        protected int currentStageIndex = -1;

        /// <summary>
        /// The stage that will be played when Deactivate() is called.
        /// </summary>
        protected SSCSeatStage deactivateStartStage = null;


        protected SSCSeatStage deactivateEndStage = null;

        protected AudioSource seatAudioSource = null;

        protected bool hasAudioClips = false;

        protected bool isActivated = false;

        protected bool isActivating = false;

        protected bool isDeactivating = false;

        protected bool isAudioAvailable = false;

        protected bool isInitialised = false;

        protected Animator animator = null;

        /// <summary>
        /// Overall volume picked up from the audio source
        /// </summary>
        protected float maxAudioVolume = 1f;

        protected int numStages = 0;

        protected int scriptInstanceID = 0;

        #endregion

        #region Public Delegates

        #endregion

        #region Private Initialise Methods

        // Use this for initialization
        void Awake()
        {
            if (initialiseOnAwake) { Initialise(); }
        }

        #endregion

        #region Protected and Internal Methods - General

        /// <summary>
        /// Attempt to initialise the seat stage
        /// </summary>
        /// <param name="seatStage"></param>
        protected void InitStage(SSCSeatStage seatStage)
        {
            seatStage.activateHash = string.IsNullOrEmpty(seatStage.activateParamName) ? 0 : Animator.StringToHash(seatStage.activateParamName);
            seatStage.deactivateHash = string.IsNullOrEmpty(seatStage.deactivateParamName) ? 0 : Animator.StringToHash(seatStage.deactivateParamName);

            seatStage.paramTypeInt = (int)seatStage.parameterType;
            seatStage.completionTypeInt = (int)seatStage.completionType;
            seatStage.completionActionInt = (int)seatStage.completionAction;

            if (seatStage.completionTypeInt == SSCSeatStage.CompletionTypeIntByDuration)
            {
                seatStage.byDurationWait = new WaitForSeconds(seatStage.stageDuration);
            }

            if (seatStage.audioClip != null) { hasAudioClips = true; }

            // Start in the closed state
            if (animator != null)
            {
                if (seatStage.paramTypeInt == ParamTypeIntTrigger)
                {
                    animator.ResetTrigger(seatStage.activateHash);

                    // For triggers we need to keep track of the status as currently no simple way to get
                    // this data from Unity.
                    seatStage.isActivated = false;
                }
                else
                {
                    animator.SetBool(seatStage.activateHash, false);
                }
            }

            seatStage.callbackOnCompletion = StageCompletion;
        }

        /// <summary>
        /// Attempt to play the audio clip for the stage
        /// </summary>
        /// <param name="seatStage"></param>
        protected void PlayAudioClip (SSCSeatStage seatStage)
        {
            if (isAudioAvailable && seatStage.audioClip != null)
            {
                seatAudioSource.clip = seatStage.audioClip;
                seatAudioSource.volume = maxAudioVolume * seatStage.maxAudioVolume;
                if (!seatAudioSource.isActiveAndEnabled) { seatAudioSource.enabled = true; }

                seatAudioSource.Play();
            }
        }

        /// <summary>
        /// Attempt to run a stage.
        /// NOTE: Triggers haven't been fully tested and may need more work.
        /// </summary>
        /// <param name="seatStage"></param>
        /// <returns></returns>
        protected IEnumerator RunStage(SSCSeatStage seatStage)
        {
            bool isTrigger = seatStage.paramTypeInt == SSCSeatAnimator.ParamTypeIntTrigger;

            // Is this bool stage animation paramater already set to the final state?
            bool isSkipAnims = !isTrigger && animator.GetBool(seatStage.activateHash) == seatStage.paramBoolValue;

            if (!isSkipAnims && isTrigger)
            {
                // Is a trigger stage already in the final state?
                isSkipAnims = (isActivating && seatStage.isActivated) || (isDeactivating && !seatStage.isActivated);
            }

            if ((seatStage.isAlwaysCallPreStage || !isSkipAnims) && seatStage.onPreStage != null)
            {
                seatStage.onPreStage.Invoke(scriptInstanceID, currentStageIndex, seatStage.guidHash, Vector3.zero);
            }

            animator.speed = isDeactivating ? seatStage.deactivateSpeed : seatStage.activateSpeed;

            if (isTrigger)
            {
                if (isActivating)
                {
                    if (!seatStage.isActivated)
                    {
                        animator.ResetTrigger(seatStage.activateHash);
                        animator.SetTrigger(seatStage.activateHash);
                        seatStage.isActivated = true;
                    }
                }
                else if (isDeactivating && seatStage.isActivated)
                {
                    animator.ResetTrigger(seatStage.deactivateHash);
                    animator.SetTrigger(seatStage.deactivateHash);
                    seatStage.isActivated = false;
                }
            }
            else if (!isSkipAnims)
            {
                animator.SetBool(seatStage.activateHash, seatStage.paramBoolValue);
            }

            if (isSkipAnims)
            {
                yield return null;
            }
            else
            {
                // If there is a clip, play it
                PlayAudioClip(seatStage);

                if (seatStage.completionTypeInt == SSCSeatStage.CompletionTypeIntByDuration)
                {
                    yield return seatStage.byDurationWait;
                }
                else
                {
                    yield return null;
                }
            }

            if (seatStage.callbackOnCompletion != null) { seatStage.callbackOnCompletion.Invoke(seatStage); }
        }

        /// <summary>
        /// Receive notification when a stage is completed.
        /// Determine if the seat is activated.
        /// Call any post event methods.
        /// If required, kick off the next stage.
        /// </summary>
        /// <param name="seatStage"></param>
        protected void StageCompletion(SSCSeatStage seatStage)
        {
            //Debug.Log("StageCompletion: " + seatStage.stageName + " currentStageIndex: " + currentStageIndex + " isActivating: " + isActivating + " isDeactivating: " + isDeactivating + " T:" + Time.time);

            bool isSkip = (isActivating && isActivated) || (isDeactivating && !isActivated);

            if (isActivating)
            {
                // Is this the last activate stage?
                // Don't make activated until the final activate stage has been completed
                if (activateEndStage != null && seatStage.guidHash == activateEndStage.guidHash)
                {
                    isActivated = true;
                    isActivating = false;
                }
            }
            else if (isDeactivating)
            {
                if (deactivateEndStage != null && seatStage.guidHash == deactivateEndStage.guidHash)
                {
                    isActivated = false;
                    isDeactivating = false;
                }
            }

            if ((seatStage.isAlwaysCallPostStage || !isSkip) && seatStage.onPostStage != null)
            {
                seatStage.onPostStage.Invoke(scriptInstanceID, currentStageIndex, seatStage.guidHash, Vector3.zero);
            }

            if (seatStage.completionActionInt == SSCSeatStage.CompletionActionIntRunNext)
            {
                if (currentStageIndex < numStages - 1)
                {
                    PlayStage(seatStageList[currentStageIndex + 1]);
                }
                else if (numStages > 1)
                {
                    // Loop back and play the first stage
                    PlayStage(seatStageList[0]);
                }
            }
        }

        #endregion

        #region Events

        protected virtual void OnDestroy()
        {
            CancelInvoke();
            RemoveListeners();
        }

        #endregion

        #region Public API Methods - General

        /// <summary>
        /// Attempt to initialise the SSCSeatAnimator component
        /// </summary>
        public virtual void Initialise()
        {
            if (!gameObject.TryGetComponent(out animator))
            {
                Debug.LogWarning("[ERROR] SSCSeatAnimator.Initialise() could not find attached Animator on " + name + " in " + gameObject.scene.name);
            }
            else
            {
                // Keep compiler happy
                if (allowRepaint) { }

                scriptInstanceID = GetInstanceID();

                numStages = seatStageList.Count;
                hasAudioClips = false;

                if (numStages > 0)
                {
                    activateStartStage = GetStageByName(activateStartStageName);
                    activateEndStage = GetStageByName(activateEndStageName);
                    deactivateStartStage = GetStageByName(deactivateStartStageName);
                    deactivateEndStage = GetStageByName(deactivateEndStageName);

                    // Initialise all the stages
                    for (int sIdx = 0; sIdx < numStages; sIdx++)
                    {
                        SSCSeatStage seatStage = seatStageList[sIdx];

                        if (seatStage != null)
                        {
                            InitStage(seatStage);
                        }
                    }
                }

                RefreshStartStages();

                ResetAudioSettings();

                isInitialised = true;
            }
        }

        /// <summary>
        /// Attempt to activate the seat my starting with the first activate stage in the sequence.
        /// </summary>
        public virtual void Activate()
        {
            if (isInitialised)
            {
                if (numStages > 0)
                {
                    isActivating = true;
                    PlayStage(activateStartStage);
                }
            }
        }

        /// <summary>
        /// Attempt to activate starting from the given zer0-based index in the stage list
        /// </summary>
        /// <param name="index"></param>
        public void ActivateStageByIndex (int index)
        {
            PlayStage(GetStageByIndex(index));
        }

        /// <summary>
        /// Add a new stage to the end of the list
        /// </summary>
        /// <param name="newStageName"></param>
        /// <returns></returns>
        public SSCSeatStage AddStage (string newStageName)
        {
            SSCSeatStage seatStage = new SSCSeatStage() { stageName = newStageName };

            seatStageList.Add(seatStage);
            numStages = seatStageList.Count;

            if (isInitialised)
            {
                InitStage(seatStage);
            }

            return seatStage;
        }

        /// <summary>
        /// Attempt to return the seat to its original position and rotation
        /// by calling the start of the deactivate sequence.
        /// </summary>
        public virtual void Deactivate()
        {
            if (isInitialised)
            {
                if (numStages > 0)
                {
                    isDeactivating = true;
                    PlayStage(deactivateStartStage);
                }
            }
        }

        /// <summary>
        /// Attempt to delete a stage at the given zero-based index in the list.
        /// </summary>
        /// <param name="deleteIndex"></param>
        /// <returns></returns>
        public bool DeleteStageAt (int deleteIndex)
        {
            numStages = seatStageList.Count;

            if (deleteIndex > -1 && deleteIndex < numStages - 1)
            {
                RemoveListeners(seatStageList[deleteIndex]);
                seatStageList.RemoveAt(deleteIndex);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Get the end or last stage in an activate sequence
        /// based on the one defined by Activate End Stage Name.
        /// </summary>
        /// <returns></returns>
        public SSCSeatStage GetEndActivateStage()
        {
            return activateEndStage;
        }

        /// <summary>
        /// Get the end or last stage in an deactivate sequence
        /// based on the one defined by Deactivate End Stage Name.
        /// </summary>
        /// <returns></returns>
        public SSCSeatStage GetEndDeactivateStage()
        {
            return deactivateEndStage;
        }

        /// <summary>
        /// Attempt to get a seat stage from the list based on the unique guidHash of the stage.
        /// </summary>
        /// <param name="guidHash"></param>
        /// <returns></returns>
        public SSCSeatStage GetStage(int guidHash)
        {
            SSCSeatStage seatStage = null;

            if (guidHash != 0)
            {
                if (!isInitialised) { numStages = seatStageList.Count; }

                for (int sIdx = 0; sIdx < numStages; sIdx++)
                {
                    SSCSeatStage _seatStage = seatStageList[sIdx];

                    if (_seatStage != null && _seatStage.guidHash == guidHash)
                    {
                        seatStage = _seatStage;
                        break;
                    }
                }
            }

            return seatStage;
        }

        /// <summary>
        /// Attempt to get the seat stage given a zero-based index from the list
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public SSCSeatStage GetStageByIndex (int index)
        {
            if (!isInitialised) { numStages = seatStageList.Count; }

            if (index >= 0 && index < numStages)
            {
                return seatStageList[index];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Attempt to get a seat stage using its name.
        /// WARNING: This may impact GC, so where possible use
        /// GetStage(..) or GetStageByIndex(..) instead.
        /// </summary>
        /// <param name="stageName"></param>
        /// <returns></returns>
        public SSCSeatStage GetStageByName (string stageName)
        {
            SSCSeatStage seatStage = null;

            if (!string.IsNullOrEmpty(stageName))
            {
                if (!isInitialised) { numStages = seatStageList.Count; }

                for (int sIdx = 0; sIdx < numStages; sIdx++)
                {
                    SSCSeatStage _seatStage = seatStageList[sIdx];

                    if (_seatStage != null && _seatStage.stageName == stageName)
                    {
                        seatStage = _seatStage;
                        break;
                    }
                }
            }

            return seatStage;
        }

        /// <summary>
        /// Attempt to find the zero-based index of the stage in the list.
        /// Will return -1 if not found.
        /// </summary>
        /// <param name="seatStage"></param>
        /// <returns></returns>
        public int GetStageIndex (SSCSeatStage seatStage)
        {
            int stageIndex = -1;

            if (seatStage != null)
            {
                if (seatStage.guidHash == 0)
                {
                    Debug.LogWarning("SSCSeatAnimator.GetStageIndex the given SSCSeatStage [" + seatStage.stageName + "] on " + name + " in " + gameObject.scene.name + " does not have a unique guidHash. It is set to 0");
                }
                else
                {

                    if (!isInitialised) { numStages = seatStageList.Count; }

                    for (int sIdx = 0; sIdx < numStages; sIdx++)
                    {
                        SSCSeatStage _seatStage = seatStageList[sIdx];

                        if (_seatStage != null && _seatStage.guidHash == seatStage.guidHash)
                        {
                            stageIndex = sIdx;
                            break;
                        }
                    }
                }
            }

            return stageIndex;
        }

        /// <summary>
        /// Attempt to get the index of the stage in the list
        /// given the name of the stage.
        /// Returns -1 if not found.
        /// </summary>
        /// <param name="stageName"></param>
        /// <returns></returns>
        public int GetStageIndexByName (string stageName)
        {
            int stageIndex = -1;

            if (!string.IsNullOrEmpty(stageName))
            {
                if (!isInitialised) { numStages = seatStageList.Count; }

                for (int sIdx = 0; sIdx < numStages; sIdx++)
                {
                    SSCSeatStage _seatStage = seatStageList[sIdx];

                    if (_seatStage != null && _seatStage.stageName == stageName)
                    {
                        stageIndex = sIdx;
                        break;
                    }
                }
            }

            return stageIndex;
        }

        /// <summary>
        /// Attempt to insert a stage into the list at a zero-based index.
        /// </summary>
        /// <param name="newSeatStage"></param>
        /// <param name="insertIndex"></param>
        /// <returns></returns>
        public bool InsertStage (SSCSeatStage newSeatStage, int insertIndex)
        {
            numStages = seatStageList.Count;

            if (numStages == 0)
            {
                seatStageList.Add(newSeatStage);
                numStages = seatStageList.Count;

                if (isInitialised) { InitStage(newSeatStage); }

                return true;
            }
            else if (insertIndex > -1)
            {
                numStages = seatStageList.Count;
                seatStageList.Insert(insertIndex, newSeatStage);

                if (seatStageList.Count > numStages)
                {
                    numStages = seatStageList.Count;
                    if (isInitialised) { InitStage(newSeatStage); }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else { return false; }
        }

        /// <summary>
        /// Attempt to play the given seat stage
        /// </summary>
        /// <param name="seatStage"></param>
        public virtual void PlayStage (SSCSeatStage seatStage)
        {
            if (isInitialised && seatStage != null)
            {
                //Debug.Log("[DEBUG] PlayStage " + seatStage.stageName);

                currentStageIndex = GetStageIndex(seatStage);
                StartCoroutine(RunStage(seatStage));
            }
        }

        /// <summary>
        /// Call this when you wish to remove any custom event listeners, like
        /// after creating them in code and then destroying the object.
        /// You could add this to your game play OnDestroy code.
        /// </summary>
        public void RemoveListeners()
        {
            if (isInitialised)
            {
                numStages = seatStageList.Count;

                for (int sIdx = 0; sIdx < numStages; sIdx++)
                {
                    RemoveListeners(seatStageList[sIdx]);
                }
            }
        }

        /// <summary>
        /// Attempt to remove any custom listeners from the given stage.
        /// </summary>
        /// <param name="seatStage"></param>
        public void RemoveListeners (SSCSeatStage seatStage)
        {
            if (seatStage != null)
            {
                if (seatStage.onPreStage != null) { seatStage.onPreStage.RemoveAllListeners(); }
                if (seatStage.onPostStage != null) { seatStage.onPostStage.RemoveAllListeners(); }
            }
        }

        /// <summary>
        /// Call after changing audio source
        /// </summary>
        public void ResetAudioSettings()
        {
            isAudioAvailable = false;

            if (gameObject.TryGetComponent(out seatAudioSource))
            {
                maxAudioVolume = seatAudioSource.volume;
                isAudioAvailable = true;
            }
            else if (hasAudioClips)
            {
                #if UNITY_EDITOR
                Debug.LogWarning("ERROR: SSCSeatAnimator - there is no AudioSource attached to " + name + ", so the audio clips will not play.");
                #endif
            }
        }

        /// <summary>
        /// Refresh which stages get called first when Activate() or Deactivate() are called.
        /// </summary>
        public void RefreshStartStages()
        {
            if (!isInitialised) { numStages = seatStageList.Count; }

            bool isActivateSet = false;
            bool isActivatedSet = false;
            bool isDeactivateSet = false;
            bool isDeactivatedSet = false;

            bool isActivateValid = !string.IsNullOrEmpty(activateStartStageName);
            bool isActivatedValid = !string.IsNullOrEmpty(activateEndStageName);
            bool isDeactivateValid = !string.IsNullOrEmpty(deactivateStartStageName);
            bool isDeactivatedValid = !string.IsNullOrEmpty(deactivateEndStageName);

            activateStartStage = null;
            activateEndStage = null;
            deactivateStartStage = null;
            deactivateEndStage = null;

            for (int sIdx = 0; sIdx < numStages; sIdx++)
            {
                SSCSeatStage seatStage = seatStageList[sIdx];

                if (seatStage != null)
                {
                    // Default to not set
                    seatStage.isActivateStage = false;
                    seatStage.isActivatedStage = false;
                    seatStage.isDeactivateStage = false;
                    seatStage.isDeactivatedStage = false;

                    if (!isActivateSet && isActivateValid && seatStage.stageName == activateStartStageName)
                    {
                        seatStage.isActivateStage = true;
                        isActivateSet = true;
                        activateStartStage = seatStage;
                    }

                    if (!isActivatedSet && isActivatedValid && seatStage.stageName == activateEndStageName)
                    {
                        seatStage.isActivatedStage = true;
                        isActivatedSet = true;
                        activateEndStage = seatStage;
                    }

                    if (!isDeactivateSet && isDeactivateValid && seatStage.stageName == deactivateStartStageName)
                    {
                        seatStage.isDeactivateStage = true;
                        isDeactivateSet = true;
                        deactivateStartStage = seatStage;
                    }

                    if (!isDeactivatedSet && isDeactivatedValid && seatStage.stageName == deactivateEndStageName)
                    {
                        seatStage.isDeactivatedStage = true;
                        isDeactivatedSet = true;
                        deactivateEndStage = seatStage;
                    }
                }
            }
        }

        /// <summary>
        /// Attempt to set the name of the stage that will be called when Activate() is called.
        /// </summary>
        /// <param name="stageName"></param>
        public void SetActivateStartStage (string stageName)
        {
            activateStartStageName = stageName;

            if (isInitialised)
            {
                activateStartStage = GetStageByName(stageName);
            }
        }

        /// <summary>
        /// Attempt to set the name of the stage that, when completed, will mark the seat as activated.
        /// </summary>
        /// <param name="stageName"></param>
        public void SetActivateEndStage (string stageName)
        {
            activateEndStageName = stageName;

            if (isInitialised)
            {
                activateEndStage = GetStageByName(stageName);
            }
        }

        /// <summary>
        /// Attempt to set the name of the stage that will be called when Deactivate() is called.
        /// </summary>
        /// <param name="stageName"></param>
        public void SetDeactivateStartStage (string stageName)
        {
            deactivateStartStageName = stageName;

            if (isInitialised)
            {
                deactivateStartStage = GetStageByName(stageName);
            }
        }

        /// <summary>
        /// Attempt to set the name of the stage that, when completed, will mark the seat as deactivated.
        /// </summary>
        /// <param name="stageName"></param>
        public void SetDeactivateEndStage (string stageName)
        {
            deactivateEndStageName = stageName;

            if (isInitialised)
            {
                deactivateEndStage = GetStageByName(stageName);
            }
        }

        /// <summary>
        /// Attempt to update teh stage duration to be used when completion type is ByDuration
        /// </summary>
        /// <param name="seatStage"></param>
        /// <param name="newDuration"></param>
        public void SetStageDuration (SSCSeatStage seatStage, float newDuration)
        {
            if (seatStage != null && newDuration >= 0f)
            {
                if (isInitialised && newDuration != seatStage.stageDuration)
                {
                    seatStage.byDurationWait = new WaitForSeconds(newDuration);
                }

                seatStage.stageDuration = newDuration;
            }
        }

        /// <summary>
        /// Attempt to toggle the state of the seat
        /// </summary>
        public void ToggleActivate()
        {
            if (isInitialised && !isActivating && !isDeactivating)
            {
                if (isActivated)
                {
                    Deactivate();
                }
                else
                {
                    Activate();
                }
            }
        }

        #endregion

    }
}