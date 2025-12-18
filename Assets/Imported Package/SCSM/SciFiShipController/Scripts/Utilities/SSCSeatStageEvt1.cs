using UnityEngine;
using UnityEngine.Events;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    /// <summary>
    /// This allows the storage of method calls with parameters in SSCSeatAnimator.cs.
    /// We need to derive it from UnityEvents so that the UnityEvent is serializable
    /// in the inspector. When parameters are used, UnityEvent is not serializable
    /// without the use of this class. We can have up to 4 parameters.
    /// </summary>
    [System.Serializable]
    public class SSCSeatStageEvt1 : UnityEvent<int, int, int, Vector3>
    {
        // T0 int - Unity SSCSeatAnimator script object InstanceID
        // T1 int - zero-based index of the seat stage
        // T2 int - guidHash or unique identifier of the SeatStage
        // T3 vector3 - FUTURE use
    }
}