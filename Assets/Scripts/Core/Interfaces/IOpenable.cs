using System;

namespace AdventureGame
{
    /// <summary>Açılabilen nesneler (kapı, sandık, geçit...).</summary>
    public interface IOpenable
    {
        bool IsOpen { get; }

        /// <summary>Açılma animasyonu tamamlandığında tetiklenir.</summary>
        event Action Opened;

        void Open();
    }
}
