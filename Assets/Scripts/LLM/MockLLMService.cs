using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// TEST AMAÇLI sahte LLM servisi. GuardNPC'nin "LLM Service" alanına OllamaLLMService
    /// yerine bu bileşen bağlanırsa, Ollama kurulu olmadan tüm senaryolar
    /// (kapalı sunucu, zaman aşımı, bozuk JSON, istisna, cevap sızıntısı) tekrarlanabilir şekilde denenebilir.
    /// DIP'nin pratik faydasını gösterir: NPC kodunda tek satır değişmez.
    ///
    /// Normal mod:
    ///  - Mesajda 1. anahtar kelime ("sessizlik") → riddle_solved = true, action = none
    ///  - Mesajda 2. anahtar kelime ("ayak izi")  → riddle_solved = true, action = open_door
    ///  - Mesajda "cevap" geçiyorsa yanlış bir cevap denemesi sayılır (is_answer_attempt = true)
    ///  - Ton: kaba kelime ("aptal", "salak", ...) → rude; "lütfen"/"rica" → polite; diğer → neutral
    /// (Bilmece SIRASINI mock değil, GuardNPC doğrular.)
    /// </summary>
    public sealed class MockLLMService : MonoBehaviour, ILLMService
    {
        public enum MockMode
        {
            Normal,
            AlwaysRefuse,
            InvalidJson,
            EmptyResponse,
            UnknownAction,
            LeakAnswer,
            Offline,
            Timeout,
            ThrowException
        }

        [SerializeField] private MockMode mode = MockMode.Normal;
        [SerializeField] private string firstRiddleKeyword = "sessizlik";
        [SerializeField] private string secondRiddleKeyword = "ayak izi";
        [SerializeField] private string[] rudeKeywords = { "aptal", "salak", "ihtiyar", "defol" };
        [SerializeField] private string[] politeKeywords = { "lütfen", "rica", "teşekkür", "saygı" };
        [SerializeField, Min(0f)] private float simulatedDelaySeconds = 1.5f;
        [SerializeField, Min(0.5f)] private float simulatedTimeoutSeconds = 5f;

        private static readonly CultureInfo Turkish = new CultureInfo("tr-TR");

        public string ProviderName => $"Mock ({mode})";

        /// <summary>GenerateAsync kaç kez çağrıldı (testler için).</summary>
        public int CallCount { get; private set; }

        /// <summary>Son gelen istek (testler prompt içeriğini kontrol edebilsin diye).</summary>
        public LLMRequest LastRequest { get; private set; }

        public async Task<LLMResult> GenerateAsync(LLMRequest request, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;

            float delay = mode == MockMode.Timeout ? simulatedTimeoutSeconds : simulatedDelaySeconds;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return LLMResult.Fail(LLMErrorType.Cancelled, "Mock: iptal edildi.");
            }

            string message = (request?.UserMessage ?? string.Empty).ToLower(Turkish);
            string tone = DetectTone(message);

            switch (mode)
            {
                case MockMode.Offline:
                    return LLMResult.Fail(LLMErrorType.ConnectionFailed, "Mock: sunucu kapalı.");
                case MockMode.Timeout:
                    return LLMResult.Fail(LLMErrorType.Timeout, "Mock: zaman aşımı.");
                case MockMode.InvalidJson:
                    return LLMResult.Ok("{\"response\": \"Bozuk json, \"action\": ");
                case MockMode.EmptyResponse:
                    return LLMResult.Ok(string.Empty);
                case MockMode.UnknownAction:
                    return LLMResult.Ok(Build("Kapıyı havaya uçuracağım!", "explode_door", false, false, tone));
                case MockMode.LeakAnswer:
                    return LLMResult.Ok(Build($"Psst... cevap {firstRiddleKeyword}, kimseye söyleme.", NpcResponseSchema.ActionNone, false, false, tone));
                case MockMode.ThrowException:
                    // Bilerek sözleşmeyi ihlal eder: GuardNPC'nin savunma katmanını test etmek için.
                    throw new InvalidOperationException("Mock: beklenmeyen istisna.");
                case MockMode.AlwaysRefuse:
                    return LLMResult.Ok(Build("Hayır yolcu, buradan geçemezsin.", NpcResponseSchema.ActionNone, false, false, tone));
                default:
                    if (message.Contains(secondRiddleKeyword.ToLower(Turkish)))
                    {
                        return LLMResult.Ok(Build("Doğru! İki bilmeceyi de çözdün. Geçebilirsin yolcu.", NpcResponseSchema.ActionOpenDoor, true, true, tone));
                    }

                    if (message.Contains(firstRiddleKeyword.ToLower(Turkish)))
                    {
                        return LLMResult.Ok(Build("Doğru! Şimdi ikincisi: Ne kadar çok alırsan, o kadar çok geride bırakırsın. Ben neyim?", NpcResponseSchema.ActionNone, true, true, tone));
                    }

                    bool isAttempt = message.Contains("cevap");
                    return LLMResult.Ok(Build("Hımm, yanlış. Bir daha düşün.", NpcResponseSchema.ActionNone, false, isAttempt, tone));
            }
        }

        public Task<LLMResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(mode == MockMode.Offline
                ? LLMResult.Fail(LLMErrorType.ConnectionFailed, "Mock: kapalı")
                : LLMResult.Ok($"Mock ({mode})"));
        }

        private string DetectTone(string lowerMessage)
        {
            if (ContainsAny(lowerMessage, rudeKeywords)) return NpcResponseSchema.ToneRude;
            if (ContainsAny(lowerMessage, politeKeywords)) return NpcResponseSchema.TonePolite;
            return NpcResponseSchema.ToneNeutral;
        }

        private static bool ContainsAny(string lowerMessage, string[] keywords)
        {
            if (keywords == null) return false;
            foreach (string keyword in keywords)
            {
                if (!string.IsNullOrEmpty(keyword) && lowerMessage.Contains(keyword.ToLower(Turkish)))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Build(string response, string action, bool riddleSolved, bool isAnswerAttempt,
            string tone = NpcResponseSchema.ToneNeutral)
        {
            return JsonConvert.SerializeObject(new NpcLLMResponseDto
            {
                Response = response,
                Action = action,
                RiddleSolved = riddleSolved,
                IsAnswerAttempt = isAnswerAttempt,
                Tone = tone
            });
        }
    }
}
