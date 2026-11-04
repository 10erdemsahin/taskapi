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
    /// LLM'in döndürmesi beklenen JSON'un birebir karşılığı:
    /// { "response": "NPC'nin diyaloğu...", "action": "open_door" | "none" }
    /// </summary>
    [Serializable]
    public sealed class NpcLLMResponseDto
    {
        [JsonProperty("response")] public string Response;
        [JsonProperty("action")] public string Action;
    }

    /// <summary>Doğrulanmış, oyunun kullanacağı alan (domain) nesnesi. Değişmezdir (immutable).</summary>
    public sealed class NpcDecision
    {
        public static readonly NpcDecision Empty = new NpcDecision(string.Empty, NpcAction.None);

        public NpcDecision(string dialogue, NpcAction action)
        {
            Dialogue = dialogue ?? string.Empty;
            Action = action;
        }

        public string Dialogue { get; }
        public NpcAction Action { get; }
    }

    /// <summary>
    /// Ollama'nın "format" alanına gönderilen JSON Schema. Model bu şemanın dışına çıkamaz:
    /// "action" yalnızca enum'daki iki değerden biri olabilir.
    /// </summary>
    public static class NpcResponseSchema
    {
        public const string ActionOpenDoor = "open_door";
        public const string ActionNone = "none";

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
                        ["description"] = "Kapı yalnızca bilmece doğru çözüldüyse 'open_door', aksi hâlde 'none'."
                    }
                },
                ["required"] = new JArray("response", "action"),
                ["additionalProperties"] = false
            };
        }
    }
}
