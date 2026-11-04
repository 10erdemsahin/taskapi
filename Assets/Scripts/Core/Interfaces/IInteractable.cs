using UnityEngine;

namespace AdventureGame
{
    /// <summary>Etkileşimi kimin başlattığını belirtir (oyuncu / otonom yoldaş).</summary>
    public enum InteractorType
    {
        Player,
        AICompanion
    }

    /// <summary>
    /// Etkileşime girilebilen her nesnenin sözleşmesi.
    /// ISP: Sadece etkileşim ile ilgili üyeleri içerir.
    /// DIP: Oyuncu ve yapay zekâ somut sınıfları (Lever, NPC...) değil bu arayüzü tanır.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Ekranda gösterilecek ipucu metni (örn. "Konuş [E]").</summary>
        string InteractionPrompt { get; }

        /// <summary>Etkileşim için durulması gereken nokta (NavMesh hedefi olarak da kullanılır).</summary>
        Transform InteractionAnchor { get; }

        /// <summary>Verilen etkileşimci türü şu an bu nesneyi kullanabilir mi?</summary>
        bool CanInteract(InteractorType interactorType);

        /// <summary>Etkileşimi gerçekleştirir. CanInteract false ise hiçbir şey yapmamalıdır.</summary>
        void Interact(GameObject interactor, InteractorType interactorType);
    }
}
