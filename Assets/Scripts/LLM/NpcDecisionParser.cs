using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// LLM'den gelen ham metni GÜVENLİ biçimde NpcDecision'a dönüştürür.
    /// Savunma katmanları:
    ///   1) Boş/null kontrolü
    ///   2) ```json ... ``` kod bloklarını ve JSON dışı ön/son metni temizleme
    ///   3) try-catch ile JSON ayrıştırma (bozuk JSON oyunu çökertmez)
    ///   4) "action" değerini BEYAZ LİSTEYE göre eşleme; bilinmeyen değer → None (güvenli varsayılan)
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

            NpcLLMResponseDto dto;
            try
            {
                dto = JsonConvert.DeserializeObject<NpcLLMResponseDto>(json);
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

            if (dto == null)
            {
                error = "JSON 'null' olarak çözüldü.";
                return false;
            }

            NpcAction action = MapAction(dto.Action, out bool isKnownAction);
            if (!isKnownAction)
            {
                Debug.LogWarning($"[NpcDecisionParser] Bilinmeyen aksiyon '{dto.Action}' → güvenli varsayılan 'none' uygulandı.");
            }

            string dialogue = dto.Response?.Trim();
            if (string.IsNullOrEmpty(dialogue))
            {
                if (action == NpcAction.None)
                {
                    error = "'response' alanı boş veya eksik.";
                    return false;
                }

                dialogue = EmptyDialogueFallback;
            }

            decision = new NpcDecision(dialogue, action);
            return true;
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
