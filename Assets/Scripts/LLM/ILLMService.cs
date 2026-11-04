using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AdventureGame
{
    public enum LLMErrorType
    {
        None,
        ConnectionFailed,
        Timeout,
        HttpError,
        InvalidResponse,
        EmptyResponse,
        Cancelled,
        Unknown
    }

    /// <summary>LLM'e gönderilecek isteğin sağlayıcıdan bağımsız temsili.</summary>
    public sealed class LLMRequest
    {
        public LLMRequest(string systemPrompt, string prompt, JObject responseSchema, string userMessage = null)
        {
            SystemPrompt = systemPrompt;
            Prompt = prompt;
            ResponseSchema = responseSchema;
            UserMessage = userMessage;
        }

        public string SystemPrompt { get; }
        public string Prompt { get; }

        /// <summary>Yapılandırılmış çıktı için JSON Schema. Null ise serbest metin istenir.</summary>
        public JObject ResponseSchema { get; }

        /// <summary>Oyuncunun son mesajı (loglama / mock servis için).</summary>
        public string UserMessage { get; }
    }

    /// <summary>
    /// Result deseni: servis istisna FIRLATMAZ; başarı/başarısızlık bilgisini değer olarak döndürür.
    /// Böylece çağıran kod try-catch yağmuruna gerek kalmadan tüm hata türlerini ele alabilir.
    /// </summary>
    public sealed class LLMResult
    {
        private LLMResult(bool success, string content, LLMErrorType errorType, string errorMessage)
        {
            Success = success;
            Content = content;
            ErrorType = errorType;
            ErrorMessage = errorMessage;
        }

        public bool Success { get; }

        /// <summary>Modelin ürettiği ham içerik (şema istendiyse JSON metni).</summary>
        public string Content { get; }

        public LLMErrorType ErrorType { get; }
        public string ErrorMessage { get; }

        public static LLMResult Ok(string content) => new LLMResult(true, content, LLMErrorType.None, null);

        public static LLMResult Fail(LLMErrorType errorType, string message) =>
            new LLMResult(false, null, errorType, message);
    }

    /// <summary>
    /// LLM servis sözleşmesi (DIP). NPC, Ollama'yı değil bu arayüzü tanır.
    /// Ollama, LM Studio, OpenAI uyumlu bir sunucu veya test için Mock servis takılabilir.
    /// SÖZLEŞME: Uygulamalar ASLA istisna fırlatmamalı, her durumda LLMResult döndürmelidir.
    /// </summary>
    public interface ILLMService
    {
        string ProviderName { get; }

        Task<LLMResult> GenerateAsync(LLMRequest request, CancellationToken cancellationToken = default);

        /// <summary>Sunucu ayakta mı ve model yüklü mü? Content alanında kısa durum metni döner.</summary>
        Task<LLMResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    }
}
