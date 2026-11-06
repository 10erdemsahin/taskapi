using UnityEngine;

namespace AdventureGame
{
    /// <summary>Toplama görevinin adı ve tamamlanınca verilen ipucu (kod değil veri).</summary>
    [CreateAssetMenu(menuName = "AdventureGame/Quest Hint", fileName = "EchoStoneHint")]
    public sealed class QuestHintData : ScriptableObject
    {
        public string questName = "Yankı Taşı";
        [TextArea(2, 6)] public string hintText = "";
        [TextArea(1, 4)] public string completionMessage = "";
    }
}
