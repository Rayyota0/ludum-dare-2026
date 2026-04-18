using System;
using UnityEngine;

namespace LudumDare.VoiceFog
{
    /// <summary>
    /// Base type for keyword sources (<see cref="SignalFogBurst"/> wires these automatically).
    /// </summary>
    public abstract class KeywordSourceBehaviour : MonoBehaviour, IVoiceKeywordSource
    {
        public event Action OnKeywordSignal;

        protected void RaiseKeywordSignal()
        {
            OnKeywordSignal?.Invoke();
        }
    }
}
