using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// LLM'den gelen ham metni GÜVENLİ biçimde NpcDecision'a dönüştürür.
    /// Savunma katmanları:
    ///   1) Boş/null kontrolü
    ///   2) ```json ... ``` kod bloklarını ve JSON dışı ön/son metni temizleme
    ///   3) try-catch ile JSON ayrıştırma (bozuk JSON oyunu çökertmez)
    ///   4) Her alan TÜRÜNE göre okunur: "response" metin olmalı; boolean alanlar yanlış türdeyse false
    ///   5) "action" BEYAZ LİSTEYE göre eşlenir; bilinmeyen değer → None (güvenli varsayılan)
    ///   6) "tone" beyaz listeye göre eşlenir; eksik/bilinmeyen → Neutral
    /// </summary>
    public static class NpcDecisionParser
    {
        private const string EmptyDialogueFallback = "...";

        private static readonly Regex CodeFenceRegex =
            new Regex(@"```[a-zA-Z]*", RegexOptions.Compiled);

        private static readonly Dictionary<string, NpcAction> ActionMap =
            new Dictionary<string, NpcAction>(StringComparer.OrdinalIgnoreCase)
            {
                { NpcResponseSchema.ActionOpenDoor, NpcAction.OpenDoor },
                { NpcResponseSchema.ActionNone, NpcAction.None }
            };

        public static bool TryParse(string rawContent, out NpcDecision decision, out string error)
        {
            decision = NpcDecision.Empty;
            error = null;

            if (string.IsNullOrWhiteSpace(rawContent))
            {
                error = "LLM boş içerik döndürdü.";
                return false;
            }

            string json = ExtractJsonObject(rawContent);
            if (json == null)
            {
                error = "Yanıtta bir JSON nesnesi bulunamadı.";
                return false;
            }

            JObject root;
            try
            {
                root = JToken.Parse(json) as JObject;
            }
            catch (JsonException exception)
            {
                error = $"JSON ayrıştırılamadı: {exception.Message}";
                return false;
            }
            catch (Exception exception)
            {
                error = $"Beklenmeyen ayrıştırma hatası: {exception.Message}";
                return false;
            }

            if (root == null)
            {
                error = "JSON bir nesne değil.";
                return false;
            }

            JToken responseToken = root["response"];
            if (responseToken != null && responseToken.Type != JTokenType.String && responseToken.Type != JTokenType.Null)
            {
                error = "'response' alanı metin değil.";
                return false;
            }

            string rawAction = root["action"]?.Type == JTokenType.String ? (string)root["action"] : null;
            NpcAction action = MapAction(rawAction, out bool isKnownAction);
            if (!isKnownAction)
            {
                Debug.LogWarning($"[NpcDecisionParser] Bilinmeyen aksiyon '{rawAction}' → güvenli varsayılan 'none' uygulandı.");
            }

            string dialogue = (responseToken?.Type == JTokenType.String ? (string)responseToken : null)?.Trim();
            if (string.IsNullOrEmpty(dialogue))
            {
                if (action == NpcAction.None)
                {
                    error = "'response' alanı boş veya eksik.";
                    return false;
                }

                dialogue = EmptyDialogueFallback;
            }

            bool riddleSolved = ReadBool(root["riddle_solved"]);
            bool isAnswerAttempt = ReadBool(root["is_answer_attempt"]);

            PlayerTone tone = ReadTone(root["tone"]);

            decision = new NpcDecision(dialogue, action, riddleSolved, isAnswerAttempt, tone);
            return true;
        }

        /// <summary>true/false boolean'ı veya "true"/"false" metnini okur; başka her şey → false.</summary>
        private static bool ReadBool(JToken token)
        {
            if (token == null) return false;

            switch (token.Type)
            {
                case JTokenType.Boolean:
                    return (bool)token;
                case JTokenType.String:
                    return bool.TryParse((string)token, out bool parsed) && parsed;
                default:
                    return false;
            }
        }

        /// <summary>"polite"/"rude"/"neutral" metnini okur (büyük/küçük harf önemsiz); başka her şey → Neutral.</summary>
        private static PlayerTone ReadTone(JToken token)
        {
            if (token == null || token.Type != JTokenType.String) return PlayerTone.Neutral;

            string raw = ((string)token).Trim();
            if (string.Equals(raw, NpcResponseSchema.TonePolite, StringComparison.OrdinalIgnoreCase)) return PlayerTone.Polite;
            if (string.Equals(raw, NpcResponseSchema.ToneRude, StringComparison.OrdinalIgnoreCase)) return PlayerTone.Rude;
            return PlayerTone.Neutral;
        }

        /// <summary>Metindeki ilk '{' ile son '}' arasını alır; kod bloğu işaretlerini temizler.</summary>
        private static string ExtractJsonObject(string raw)
        {
            string cleaned = CodeFenceRegex.Replace(raw, string.Empty).Trim();

            int start = cleaned.IndexOf('{');
            int end = cleaned.LastIndexOf('}');

            if (start < 0 || end <= start)
            {
                return null;
            }

            return cleaned.Substring(start, end - start + 1);
        }

        private static NpcAction MapAction(string rawAction, out bool isKnown)
        {
            if (string.IsNullOrWhiteSpace(rawAction))
            {
                isKnown = false;
                return NpcAction.None;
            }

            string normalized = rawAction.Trim().Replace('-', '_').Replace(' ', '_');
            isKnown = ActionMap.TryGetValue(normalized, out NpcAction action);
            return isKnown ? action : NpcAction.None;
        }
    }
}
