using System;

namespace AdventureGame
{
    /// <summary>
    /// Bir tetikleyici (şalter, buton, basınç plakası) tarafından çalıştırılabilen mekanizma.
    /// Örn: DrawBridge. Şalter, köprünün somut tipini bilmez (OCP / DIP).
    /// </summary>
    public interface IActivatable
    {
        bool IsActivated { get; }

        /// <summary>Mekanizma tamamen çalıştığında (animasyon + NavMesh güncellemesi bitince) tetiklenir.</summary>
        event Action Activated;

        void Activate();
    }
}
