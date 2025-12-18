using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    /// <summary>
    /// A stage or phase in a multi-stage seat animation sequence.
    /// </summary>
    [System.Serializable]
    public class SSCSeatStage
    {
        #region Enumerations
        public enum CompletionType
        {
            ByDuration = 0
        }


        public enum CompletionAction
        {
            DoNothing = 0,
            RunNext = 1
        }

        #endregion

        #region Static Variables

        public static readonly int CompletionTypeIntByDuration = (int)CompletionType.ByDuration;

        public static readonly int CompletionActionIntDoNothing = (int)CompletionAction.DoNothing;
        public static readonly int CompletionActionIntRunNext = (int)CompletionAction.RunNext;

        #endregion

        #region Public Variables
        // IMPORTANT - when changing this section also update SetClassDefault()
        // Also update ClassName(ClassName className) Clone Constructor (if there is one)

        /// <summary>
        /// Unique identifier for this SeatStage
        /// </summary>
        public int guidHash;

        /// <summary>
        /// The speed at which the activate animation runs
        /// </summary>
        [Range(0.01f, 10f)] public float activateSpeed;

        /// <summary>
        /// bool or trigger Animimation Parameters to control the seat.
        /// </summary>
        public string activateParamName;

        /// <summary>
        /// The audio clip that is played when the stage is played
        /// </summary>
        public AudioClip audioClip;

        /// <summary>
        /// The hashed value of the activateParamName
        /// </summary>
        [System.NonSerialized] public int activateHash;

        /// <summary>
        /// The speed at which the deactivate animation runs
        /// </summary>
        [Range(0.01f, 10f)] public float deactivateSpeed;

        /// <summary>
        /// Trigger Animation Parameters to control the seat in your animator controller
        /// </summary>
        public string deactivateParamName;

        /// <summary>
        /// The hashed value of the deactivateParamName
        /// </summary>
        [System.NonSerialized] public int deactivateHash;

        /// <summary>
        /// What do to when the stage is completed
        /// </summary>
        public CompletionAction completionAction;

        [System.NonSerialized] public int completionActionInt;

        /// <summary>
        /// How to determine when the stage has been completed
        /// </summary>
        public CompletionType completionType;

        public int completionTypeInt;

        /// <summary>
        /// The maximum volume of the audio clip relative to the initial AudioSource volume
        /// </summary>
        [Range(0.0f, 1f)] public float maxAudioVolume;

        public SSCSeatAnimator.ParameterType parameterType;

        /// <summary>
        /// If the parameter type is Bool, it's value for this stage
        /// </summary>
        public bool paramBoolValue;

        [System.NonSerialized] public int paramTypeInt;

        // used for triggers
        public bool isActivated;

        /// <summary>
        /// If the stage is already in the final expected state, should the onPreStage methods always be called.
        /// </summary>
        public bool isAlwaysCallPreStage;

        /// <summary>
        /// If the stage is already in the final expected state, should the onPostStage methods always be called.
        /// </summary>
        public bool isAlwaysCallPostStage;

        /// <summary>
        /// Whether the stage is shown as expanded in the inspector window of the editor.
        /// </summary>
        public bool showInEditor;

        /// <summary>
        /// These get called immediately before the stage is played
        /// </summary>
        public SSCSeatStageEvt1 onPreStage;

        /// <summary>
        /// These get called immediately after the stage completes but before the completion action is run.
        /// </summary>
        public SSCSeatStageEvt1 onPostStage;

        /// <summary>
        /// How many seconds the stage should take to complete when completion type is ByDuration.
        /// </summary>
        public float stageDuration;

        /// <summary>
        /// A descriptive name of the stage
        /// </summary>
        public string stageName;

        /// <summary>
        /// Used at runtime to reduce GC impact
        /// </summary>
        [System.NonSerialized] public WaitForSeconds byDurationWait = null;

        [System.NonSerialized] public bool isActivateStage;
        [System.NonSerialized] public bool isActivatedStage;
        [System.NonSerialized] public bool isDeactivateStage;
        [System.NonSerialized] public bool isDeactivatedStage;

        #endregion

        #region Delegates

        /// <summary>
        /// [INTERNAL ONLY]
        /// </summary>
        /// <param name="seatStage"></param>
        public delegate void CallbackOnCompletion(SSCSeatStage seatStage);

        /// <summary>
        /// [INTERNAL ONLY]
        /// To get notifications, see OnPre/Post events.
        /// </summary>
        public CallbackOnCompletion callbackOnCompletion;

        #endregion

        #region Constructors
        public SSCSeatStage()
        {
            SetClassDefaults();
        }

        #endregion

        #region Public Member Methods

        /// <summary>
        /// Set the defaults values for this class
        /// </summary>
        public void SetClassDefaults()
        {
            guidHash = SSCMath.GetHashCodeFromGuid();
            activateSpeed = 1f;
            deactivateSpeed = 1f;
            maxAudioVolume = 1f;
            paramTypeInt = 0;
            paramBoolValue = true;
            activateHash = 0;
            deactivateHash = 0;
            isActivated = false;
            completionType = CompletionType.ByDuration;
            stageName = "stage x";
            parameterType = SSCSeatAnimator.ParameterType.Bool;
            activateParamName = "isActivate";
            deactivateParamName = string.Empty;
            stageDuration = 1f;
            audioClip = null;

            isActivateStage = false;
            isDeactivateStage = false;

            showInEditor = true;
        }

        #endregion
    }
}