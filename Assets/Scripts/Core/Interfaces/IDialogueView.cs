using System;

namespace AdventureGame
{
    public enum DialogueSpeaker
    {
        Player,
        Npc,
        System
    }

    /// <summary>
    /// Diyalog arayüzünün soyutlaması. NPC mantığı UI teknolojisini (uGUI, UI Toolkit,
    /// konsol...) bilmez; sadece bu sözleşme üzerinden konuşur (DIP, SRP).
    /// </summary>
    public interface IDialogueView
    {
        /// <summary>Oyuncu bir mesaj gönderdiğinde tetiklenir.</summary>
        event Action<string> MessageSubmitted;

        /// <summary>Oyuncu pencereyi kapatmak istediğinde (Esc / Kapat butonu) tetiklenir.</summary>
        event Action CloseRequested;

        bool IsOpen { get; }

        void Open(string title);
        void Close();
        void ClearMessages();
        void AppendMessage(DialogueSpeaker speaker, string speakerName, string text);

        /// <summary>"NPC düşünüyor..." göstergesini açar/kapatır.</summary>
        void SetThinking(bool isThinking, string label = null);

        void SetInputEnabled(bool isEnabled);
    }
}
