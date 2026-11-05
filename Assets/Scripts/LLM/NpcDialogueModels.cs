using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AdventureGame
{
    /// <summary>Oyunun tanıdığı ve uygulayabildiği NPC aksiyonları (beyaz liste).</summary>
    public enum NpcAction
    {
        None,
        OpenDoor
    }

    /// <summary>
    /// Oyuncunun mesajının tonu (LLM sınıflandırır). Güven puanını LLM DEĞİL Unity hesaplar:
    /// LLM yalnızca tonu söyler, puan değişimi persona'daki sabit değerlerle uygulanır.
    /// </summary>
    public enum PlayerTone
    {
        Neutral,
        Polite,
        Rude
    }

    /// <summary>
    /// LLM'in döndürmesi beklenen JSON'un birebir karşılığı:
    /// { "response": "...", "action": "open_door" | "none", "riddle_solved": bool, "is_answer_attempt": bool,
    ///   "tone": "polite" | "neutral" | "rude" }
    /// </summary>
    [Serializable]
    public sealed class NpcLLMResponseDto
    {
        [JsonProperty("response")] public string Response;
        [JsonProperty("action")] public string Action;
        [JsonProperty("riddle_solved")] public bool? RiddleSolved;
        [JsonProperty("is_answer_attempt")] public bool? IsAnswerAttempt;
        [JsonProperty("tone")] public string Tone;
    }

    /// <summary>Doğrulanmış, oyunun kullanacağı alan (domain) nesnesi. Değişmezdir (immutable).</summary>
    public sealed class NpcDecision
    {
        public static readonly NpcDecision Empty = new NpcDecision(string.Empty, NpcAction.None);

        public NpcDecision(string dialogue, NpcAction action, bool riddleSolved = false, bool isAnswerAttempt = false,
            PlayerTone tone = PlayerTone.Neutral)
        {
            Dialogue = dialogue ?? string.Empty;
            Action = action;
            RiddleSolved = riddleSolved;
            IsAnswerAttempt = isAnswerAttempt;
            Tone = tone;
        }

        public string Dialogue { get; }
        public NpcAction Action { get; }

        /// <summary>LLM'e göre oyuncu SIRADAKİ bilmeceyi bu mesajla çözdü mü? (Unity ayrıca doğrular.)</summary>
        public bool RiddleSolved { get; }

        /// <summary>LLM'e göre oyuncunun mesajı bir cevap denemesi mi? (Deneme hakkı sayacı için.)</summary>
        public bool IsAnswerAttempt { get; }

        /// <summary>LLM'e göre oyuncunun mesajının tonu (güven puanı için). Bilinmiyorsa Neutral.</summary>
        public PlayerTone Tone { get; }
    }

    /// <summary>
    /// Ollama'nın "format" alanına gönderilen JSON Schema. Model bu şemanın dışına çıkamaz:
    /// "action" yalnızca enum'daki iki değerden biri olabilir, boolean alanlar boolean olmak zorundadır.
    /// </summary>
    public static class NpcResponseSchema
    {
        public const string ActionOpenDoor = "open_door";
        public const string ActionNone = "none";
        public const string TonePolite = "polite";
        public const string ToneNeutral = "neutral";
        public const string ToneRude = "rude";

        public static JObject Create()
        {
            return new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["response"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "NPC'nin oyuncuya söylediği, karakterine uygun Türkçe cümle(ler)."
                    },
                    ["action"] = new JObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JArray(ActionOpenDoor, ActionNone),
                        ["description"] = "Kapı yalnızca SON bilmece doğru çözüldüyse 'open_door', aksi hâlde 'none'."
                    },
                    ["riddle_solved"] = new JObject
                    {
                        ["type"] = "boolean",
                        ["description"] = "Oyuncu SIRADAKİ bilmecenin doğru cevabını bu mesajda verdiyse true."
                    },
                    ["is_answer_attempt"] = new JObject
                    {
                        ["type"] = "boolean",
                        ["description"] = "Oyuncunun mesajı bilmeceye bir cevap denemesiyse true; sohbet/soru ise false."
                    },
                    ["tone"] = new JObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JArray(TonePolite, ToneNeutral, ToneRude),
                        ["description"] = "Oyuncunun SON mesajının tonu: saygılı/kibar ise 'polite', hakaret/tehdit/kabalık varsa 'rude', diğer durumlarda 'neutral'."
                    }
                },
                ["required"] = new JArray("response", "action", "riddle_solved", "is_answer_attempt", "tone"),
                ["additionalProperties"] = false
            };
        }
    }
}
