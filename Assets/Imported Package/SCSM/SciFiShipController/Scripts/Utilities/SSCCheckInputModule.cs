using UnityEngine;
using UnityEngine.EventSystems;
#if SSC_UIS
using UnityEngine.InputSystem.UI;
#endif

// Sci-Fi Ship Controller. Copyright (c) 2018-2025 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    /// <summary>
    /// Check that the attached EventSystem has the correct input module attached.
    /// Only runs at design time. Has no effect at runtime.
    /// </summary>
    [ExecuteInEditMode]
    [HelpURL("https://scsmmedia.com/ssc-documentation")]
    public class SSCCheckInputModule : MonoBehaviour
    {
        #region Public Variables

        [Tooltip("Remove this component once check is completed")]
        public bool destroyWhenDone = false;

        [Tooltip("If both legacy and new Input System present, prioritise using new")]
        public bool prioritiseNewInput = true;

        #endregion

        #region Private Variables - General

        #if SSC_UIS
        private bool isUIS = true;
        #else
        private bool isUIS = false;
        #endif

        #if ENABLE_LEGACY_INPUT_MANAGER
        private bool isLegacyInput = true;
        #else
        private bool isLegacyInput = false;
        #endif

        #endregion

        #region Private Initialise Methods

        // Use this for initialization
        void Start()
        {
            // Only run at design time
            if (Application.isPlaying) { return; }

            EventSystem evtSystem;
            StandaloneInputModule legacyInputModule = null;

            if (!TryGetComponent(out evtSystem))
            {
                Debug.LogWarning("[ERROR] SSCCheckInputModule is not attached to an EventSystem component. Removing.");

                RemoveNow();
            }
            else if ((!isLegacyInput || (isUIS && prioritiseNewInput)) && TryGetComponent(out legacyInputModule))
            {
                Debug.Log("[INFO] SSCCheckInputModule Legacy Input is not enabled, so removing StandaloneInputModule from " + gameObject.name);

                #if UNITY_EDITOR
                UnityEditor.Undo.DestroyObjectImmediate(legacyInputModule);
                #else
                Destroy(legacyInputModule);
                #endif
            }

            // If new Input System is installed add the module
            #if SSC_UIS

            InputSystemUIInputModule newInputModule;

            if (!TryGetComponent(out newInputModule))
            {
                Debug.Log("[INFO] SSCCheckInputModule (new) Input Module is not present, so adding InputSystemUIInputModule to " + gameObject.name);
                gameObject.AddComponent<InputSystemUIInputModule>();
                #if UNITY_EDITOR
                UnityEditor.Undo.RegisterCreatedObjectUndo(gameObject, "Add Input System UI Module");
                #endif
            }
            #else

             // Not UIS with Legacy installed but no legacy StandaloneInputModule
            if (isLegacyInput && !TryGetComponent(out legacyInputModule))
            {
                gameObject.AddComponent<StandaloneInputModule>();
                #if UNITY_EDITOR
                UnityEditor.Undo.RegisterCreatedObjectUndo(gameObject, "Add StandaloneInputModule");
                #endif
            }

            #endif

            #if UNITY_EDITOR
            UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
            #endif

            if (destroyWhenDone) { RemoveNow(); }
        }


        private void RemoveNow()
        {
            #if UNITY_EDITOR
            DestroyImmediate(this);
            #else
            Destroy(this);
            #endif
        }


        #endregion
    }
}