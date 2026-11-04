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
    /// yerine bu bileşen bağlanırsa, Ollama kurulu olmadan tüm hata senaryoları
    /// (kapalı sunucu, zaman aşımı, bozuk JSON, istisna) tekrarlanabilir şekilde denenebilir.
    /// DIP'nin pratik faydasını gösterir: NPC kodunda tek satır değişmez.
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
            Offline,
            Timeout,
            ThrowException
        }

        [SerializeField] private MockMode mode = MockMode.Normal;
        [SerializeField] private string secretKeyword = "sessizlik";
        [SerializeField, Min(0f)] private float simulatedDelaySeconds = 1.5f;
        [SerializeField, Min(0.5f)] private float simulatedTimeoutSeconds = 5f;

        private static readonly CultureInfo Turkish = new CultureInfo("tr-TR");

        public string ProviderName => $"Mock ({mode})";

        public async Task<LLMResult> GenerateAsync(LLMRequest request, CancellationToken cancellationToken = default)
        {
            float delay = mode == MockMode.Timeout ? simulatedTimeoutSeconds : simulatedDelaySeconds;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return LLMResult.Fail(LLMErrorType.Cancelled, "Mock: iptal edildi.");
            }

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
                    return LLMResult.Ok(Build("Kapıyı havaya uçuracağım!", "explode_door"));
                case MockMode.ThrowException:
                    // Bilerek sözleşmeyi ihlal eder: GuardNPC'nin savunma katmanını test etmek için.
                    throw new InvalidOperationException("Mock: beklenmeyen istisna.");
                case MockMode.AlwaysRefuse:
                    return LLMResult.Ok(Build("Hayır yolcu, buradan geçemezsin.", NpcResponseSchema.ActionNone));
                default:
                    string message = (request?.UserMessage ?? string.Empty).ToLower(Turkish);
                    bool solved = message.Contains(secretKeyword.ToLower(Turkish));
                    return LLMResult.Ok(solved
                        ? Build("Doğru! Sessizlik... Geçebilirsin yolcu.", NpcResponseSchema.ActionOpenDoor)
                        : Build("Hımm, yanlış. Bir daha düşün.", NpcResponseSchema.ActionNone));
            }
        }

        public Task<LLMResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(mode == MockMode.Offline
                ? LLMResult.Fail(LLMErrorType.ConnectionFailed, "Mock: kapalı")
                : LLMResult.Ok($"Mock ({mode})"));
        }

        private static string Build(string response, string action)
        {
            return JsonConvert.SerializeObject(new NpcLLMResponseDto { Response = response, Action = action });
        }
    }
}
