using System;

namespace AdventureGame
{
    /// <summary>
    /// Açık/kapalı durumu olan bir girdi (basınç plakası, düğme, sensör).
    /// AndGate gibi mantık kapıları somut tipi bilmeden bu arayüzü dinler.
    /// </summary>
    public interface IToggleSignal
    {
        bool IsOn { get; }
        event Action<bool> Changed;
    }
}
