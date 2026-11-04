using System;

namespace AdventureGame
{
    /// <summary>
    /// Konuşabilen NPC sözleşmesi. NPC kapıyı DOĞRUDAN açmaz; sadece "aksiyon isteği"
    /// yayınlar. Oyun kurallarına göre isteği onaylamak GameManager'ın işidir
    /// (LLM çıktısına körü körüne güvenmeme ilkesi).
    /// </summary>
    public interface IConversationalNpc
    {
        string DisplayName { get; }

        /// <summary>true: diyalog açıldı, false: diyalog kapandı.</summary>
        event Action<bool> ConversationStateChanged;

        /// <summary>LLM'in döndürdüğü ve doğrulanmış aksiyon.</summary>
        event Action<NpcAction> ActionRequested;
    }
}
