using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sci-Fi Ship Controller. Copyright (c) 2018-2025 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    /// <summary>
    /// Base scriptable lookup for key (int)-values (string) set.
    /// Key = 0 will always return Default
    /// </summary>
    //[CreateAssetMenu(fileName = "SSC Lookup", menuName = "Sci-Fi Ship Controller/Lookup")]
    [HelpURL("https://scsmmedia.com/ssc-documentation")]
    public class SSCLookup : ScriptableObject
    {
        #region Protected Variables

        /// <summary>
        /// The array of lookup values or descriptions
        /// </summary>
        [SerializeField] protected string[] lookupValues;

        /// <summary>
        /// [INTERNAL ONLY]
        /// </summary>
        [SerializeField] protected bool isExpandedInEditor = true;

        #endregion

        #region Public Non-Virtual Properties

        /// <summary>
        /// Get the number of custom lookups
        /// </summary>
        public int NumberOfLookups { get => lookupValues == null ? 0 : lookupValues.Length; }

        #endregion

        #region Public Virtual Properties

        /// <summary>
        /// The overrideable element label width in the editor
        /// </summary>
        public virtual float ElementLabelWidth { get => 90f; }

        /// <summary>
        /// The overrideable prefix to the left of each lookup value
        /// </summary>
        public virtual string ElementPrefix { get => "Item"; }

        /// <summary>
        /// The overrideable default value
        /// </summary>
        public virtual string DefaultValue { get => "Default"; }

        /// <summary>
        /// The short display name for the array of lookups
        /// </summary>
        public virtual string LookupsShortName { get => "Custom Lookups"; }

        #endregion

        #region Public API Methods

        /// <summary>
        /// Does this lookup ScriptableObject contain the given key?
        /// A key of 0 (the default) will always return true.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Contains (int key)
        {
            return key == 0 || (key > 0 && key <= NumberOfLookups);
        }

        /// <summary>
        /// Does this ScriptableObject contain the given custom value?
        /// Will return false if not found or if it equals the DefaultValue
        /// </summary>
        /// <param name="lookupValue"></param>
        /// <returns></returns>
        public bool Contains (string lookupValue)
        {
            return GetFirstKey(lookupValue) != 0;
        }

        /// <summary>
        /// Get the first matching lookup value, otherwise return the DefaultValue.
        /// </summary>
        /// <param name="lookupValue"></param>
        /// <returns></returns>
        public int GetFirstKey (string lookupValue)
        {
            int key = 0;
            int numValues = NumberOfLookups;

            // Ignore the default key
            if (numValues > 0 && lookupValue != DefaultValue)
            {
                for (int kIdx = 0; kIdx < numValues; kIdx++)
                {
                    if (lookupValues[kIdx] == lookupValue)
                    {
                        key = kIdx + 1;
                        break;
                    }
                }
            }

            return key;
        }

        /// <summary>
        /// Attempt to get the value of a key.
        /// If key = 0 or outside range, will return the DefaultValue
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetValue (int key)
        {
            int numValues = NumberOfLookups;

            if (key > 0 && key <= numValues)
            {
                string lookupValue = lookupValues[key - 1];
                return string.IsNullOrEmpty(lookupValue) ? key + " unknown" : lookupValue;
            }
            else { return DefaultValue; }
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Get the keys for a given Lookup ScriptableObject.
        /// Always return a Default key.
        /// </summary>
        /// <param name="sscLookup"></param>
        /// <returns></returns>
        public static int[] GetKeys (SSCLookup sscLookup)
        {
            int numValues = sscLookup == null ? 0 : sscLookup.NumberOfLookups;

            // Always add the Default key
            int[] keys = new int[numValues + 1];

            keys[0] = 0;

            for (int kIdx = 1; kIdx < numValues + 1; kIdx++)
            {
                keys[kIdx] = kIdx;
            }

            return keys;
        }

        /// <summary>
        /// Get the values for a given Lookup ScriptableObject.
        /// Always returns the Default value for the first entry.
        /// </summary>
        /// <param name="sscLookup"></param>
        /// <returns></returns>
        public static string[] GetValues (SSCLookup sscLookup)
        {
            int numValues = sscLookup == null ? 0 : sscLookup.NumberOfLookups;

            // Always add the Default Value
            string[] lkpValues = new string[numValues + 1];

            lkpValues[0] = sscLookup == null ? "Default" : sscLookup.DefaultValue;

            for (int kIdx = 1; kIdx < numValues + 1; kIdx++)
            {
                lkpValues[kIdx] = sscLookup.GetValue(kIdx);

                //Debug.Log("key " + kIdx + " value: " + lkpValues[kIdx]);
            }

            return lkpValues;
        }

        #endregion

    }
}