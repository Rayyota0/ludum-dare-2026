using System;

namespace LudumDare.VoiceFog
{
    /// <summary>
    /// Cross-platform keyword trigger (implemented by <see cref="KeywordSourceBehaviour"/> derivatives).
    /// </summary>
    public interface IVoiceKeywordSource
    {
        event Action OnKeywordSignal;
    }
}
