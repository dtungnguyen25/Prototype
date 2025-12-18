using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sci-Fi Ship Controller. Copyright (c) 2018-2025 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    /// <summary>
    /// For making and using animation curves
    /// </summary>
    public class SSCCurve
    {
        #region Enumerations

        public enum CurvePreset
        {
            None = -1,
            Linear = 0,
            LinearDecline = 1,
            EaseInAndOut = 2,
            EaseOutAndIn = 3,
            Constant = 4,
            PowerOfOnePointFive = 12,
            PowerOfOnePointFiveDecline = 13,
            Squared = 16,
            SquaredDecline = 17,
            Cubed = 18,
            CubedDecline = 19,
            PowerOfFour = 20,
            PowerOfFourDecline = 21
        }

        #endregion

        #region Protected Variables

        protected static int keyInt;

        #endregion

        #region Public Methods

        /// <summary>
        /// Given a curve preset, return the animation curve.
        /// </summary>
        /// <param name="curvePreset"></param>
        /// <returns></returns>
        public static AnimationCurve GetCurveFromPreset(SSCCurve.CurvePreset curvePreset)
        {
            AnimationCurve newCurve = new AnimationCurve();
            if (curvePreset == CurvePreset.Linear)
            {
                newCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            }
            else if (curvePreset == CurvePreset.EaseInAndOut)
            {
                newCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }
            else if (curvePreset == CurvePreset.EaseOutAndIn)
            {
                newCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            }
            else if (curvePreset == CurvePreset.Constant || curvePreset == CurvePreset.None)
            {
                newCurve = AnimationCurve.Constant(0f, 1f, 1f);
            }
            else if (curvePreset == CurvePreset.LinearDecline)
            {
                newCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            }
            else if (curvePreset == CurvePreset.PowerOfOnePointFive)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.5f, 0.35f);
                keyInt = newCurve.AddKey(1f, 1f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 1.06f;
                curveKeys[1].outTangent = 1.06f;
                curveKeys[2].inTangent = 1.5f;
                curveKeys[2].outTangent = 1.5f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.PowerOfOnePointFiveDecline)
            {
                keyInt = newCurve.AddKey(0f, 1f);
                keyInt = newCurve.AddKey(0.5f, 0.35f);
                keyInt = newCurve.AddKey(1f, 0f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = -1.5f;
                curveKeys[0].outTangent = -1.5f;
                curveKeys[1].inTangent = -1.06f;
                curveKeys[1].outTangent = -1.06f;
                curveKeys[2].inTangent = 0f;
                curveKeys[2].outTangent = 0f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.Squared)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.5f, 0.25f);
                keyInt = newCurve.AddKey(1f, 1f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 1f;
                curveKeys[1].outTangent = 1f;
                curveKeys[2].inTangent = 2f;
                curveKeys[2].outTangent = 2f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.SquaredDecline)
            {
                keyInt = newCurve.AddKey(0f, 1f);
                keyInt = newCurve.AddKey(0.5f, 0.25f);
                keyInt = newCurve.AddKey(1f, 0f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = -2f;
                curveKeys[0].outTangent = -2f;
                curveKeys[1].inTangent = -1f;
                curveKeys[1].outTangent = -1f;
                curveKeys[2].inTangent = 0f;
                curveKeys[2].outTangent = 0f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.Cubed)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.5f, 0.125f);
                keyInt = newCurve.AddKey(1f, 1f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 0.75f;
                curveKeys[1].outTangent = 0.75f;
                curveKeys[2].inTangent = 3f;
                curveKeys[2].outTangent = 3f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.CubedDecline)
            {
                keyInt = newCurve.AddKey(0f, 1f);
                keyInt = newCurve.AddKey(0.5f, 0.125f);
                keyInt = newCurve.AddKey(1f, 0f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = -3f;
                curveKeys[0].outTangent = -3f;
                curveKeys[1].inTangent = -0.75f;
                curveKeys[1].outTangent = -0.75f;
                curveKeys[2].inTangent = 0f;
                curveKeys[2].outTangent = 0f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.PowerOfFour)
            {
                keyInt = newCurve.AddKey(0f, 0f);
                keyInt = newCurve.AddKey(0.5f, 0.0625f);
                keyInt = newCurve.AddKey(1f, 1f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = 0f;
                curveKeys[0].outTangent = 0f;
                curveKeys[1].inTangent = 0.5f;
                curveKeys[1].outTangent = 0.5f;
                curveKeys[2].inTangent = 4f;
                curveKeys[2].outTangent = 4f;
                newCurve = new AnimationCurve(curveKeys);
            }
            else if (curvePreset == CurvePreset.PowerOfFourDecline)
            {
                keyInt = newCurve.AddKey(0f, 1f);
                keyInt = newCurve.AddKey(0.5f, 0.0625f);
                keyInt = newCurve.AddKey(1f, 0f);
                Keyframe[] curveKeys = newCurve.keys;
                curveKeys[0].inTangent = -4f;
                curveKeys[0].outTangent = -4f;
                curveKeys[1].inTangent = -0.5f;
                curveKeys[1].outTangent = -0.5f;
                curveKeys[2].inTangent = 0f;
                curveKeys[2].outTangent = 0f;
                newCurve = new AnimationCurve(curveKeys);
            }

            return newCurve;
        }

        #endregion
    }
}