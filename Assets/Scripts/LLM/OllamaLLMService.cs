using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace AdventureGame
{
    /// <summary>
    /// "OllamaManager": Yerel Ollama sunucusu ile konuşan ILLMService uygulaması.
    ///  - İstekler UnityWebRequest + async/await ile ANA THREAD'İ BLOKLAMADAN gönderilir.
    ///  - "format" alanına JSON Schema verilerek yapılandırılmış çıktı zorlanır.
    ///  - Zaman aşımı iki katmanlıdır: CancellationToken (CancelAfter) + UnityWebRequest.timeout.
    ///  - Hiçbir koşulda istisna fırlatmaz; tüm hatalar LLMResult.Fail olarak döner.
    /// </summary>
    public sealed class OllamaLLMService : MonoBehaviour, ILLMService
    {
        [Header("Sunucu")]
        [SerializeField] private string baseUrl = "http://localhost:11434";
        [SerializeField] private string generatePath = "/api/generate";
        [SerializeField] private string tagsPath = "/api/tags";

        [Header("Model")]
        [Tooltip("Önce terminalde: ollama pull <model>")]
        [SerializeField] private string model = "qwen2.5:7b";
        [SerializeField, Range(0f, 2f)] private float temperature = 0.6f;
        [SerializeField, Range(32, 2048)] private int maxTokens = 300;
        [SerializeField] private string keepAlive = "10m";

        [Header("Dayanıklılık")]
        [Tooltip("İlk istekte model belleğe yükleneceği için yüksek tutun.")]
        [SerializeField, Range(3, 300)] private int requestTimeoutSeconds = 60;
        [SerializeField, Range(1, 30)] private int healthCheckTimeoutSeconds = 3;

        [Header("Hata Ayıklama")]
        [SerializeField] private bool logTraffic = true;

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        private CancellationTokenSource _lifetimeCts;

        public string ProviderName => $"Ollama ({model})";

        private void Awake()
        {
            _lifetimeCts = new CancellationTokenSource();
        }

        private void OnDestroy()
        {
            // Sahne kapanırken bekleyen tüm istekleri iptal et.
            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
            _lifetimeCts = null;
        }

        #region ILLMService

        public async Task<LLMResult> GenerateAsync(LLMRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
                {
                    return LLMResult.Fail(LLMErrorType.Unknown, "Boş istek gönderilemez.");
                }

                var payload = new OllamaGenerateRequest
                {
                    Model = model,
                    Prompt = request.Prompt,
                    SystemPrompt = request.SystemPrompt,
                    Stream = false,
                    Format = request.ResponseSchema,
                    KeepAlive = string.IsNullOrWhiteSpace(keepAlive) ? null : keepAlive,
                    Options = new OllamaOptions { Temperature = temperature, NumPredict = maxTokens }
                };

                string json = JsonConvert.SerializeObject(payload, SerializerSettings);
                string url = CombineUrl(baseUrl, generatePath);

                if (logTraffic)
                {
                    Debug.Log($"[Ollama] → POST {url}\n{json}", this);
                }

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(requestTimeoutSeconds));
                using var linkedCts = CreateLinkedSource(cancellationToken, timeoutCts.Token);
                using var webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
                {
                    uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                    downloadHandler = new DownloadHandlerBuffer(),
                    timeout = requestTimeoutSeconds + 5 // Yedek zaman aşımı (CTS çalışmazsa).
                };
                webRequest.SetRequestHeader("Content-Type", "application/json");

                float startTime = Time.realtimeSinceStartup;

                try
                {
                    await webRequest.SendAsync(linkedCts.Token);
                }
                catch (OperationCanceledException)
                {
                    return BuildCancellationResult(cancellationToken);
                }

                if (linkedCts.IsCancellationRequested)
                {
                    return BuildCancellationResult(cancellationToken);
                }

                return InterpretGenerateResponse(webRequest, url, Time.realtimeSinceStartup - startTime);
            }
            catch (Exception exception)
            {
                // Son savunma hattı: servis sözleşmesi gereği asla dışarı istisna sızdırma.
                Debug.LogException(exception, this);
                return LLMResult.Fail(LLMErrorType.Unknown, exception.Message);
            }
        }

        public async Task<LLMResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(healthCheckTimeoutSeconds));
                using var linkedCts = CreateLinkedSource(cancellationToken, timeoutCts.Token);
                using var webRequest = UnityWebRequest.Get(CombineUrl(baseUrl, tagsPath));
                webRequest.timeout = healthCheckTimeoutSeconds + 2;

                try
                {
                    await webRequest.SendAsync(linkedCts.Token);
                }
                catch (OperationCanceledException)
                {
                    return LLMResult.Fail(LLMErrorType.Timeout, "Sunucu yanıt vermiyor");
                }

                if (linkedCts.IsCancellationRequested || webRequest.result != UnityWebRequest.Result.Success)
                {
                    return LLMResult.Fail(LLMErrorType.ConnectionFailed, "Kapalı ('ollama serve' çalıştırın)");
                }

                OllamaTagsResponse tags = JsonConvert.DeserializeObject<OllamaTagsResponse>(webRequest.downloadHandler.text);
                bool hasModel = tags?.Models != null && tags.Models.Exists(m => m != null && IsSameModel(m.Name, model));

                return hasModel
                    ? LLMResult.Ok($"Bağlı ({model})")
                    : LLMResult.Fail(LLMErrorType.HttpError, $"'{model}' yüklü değil → ollama pull {model}");
            }
            catch (Exception exception)
            {
                return LLMResult.Fail(LLMErrorType.Unknown, exception.Message);
            }
        }

        #endregion

        #region Yanıt Yorumlama

        private LLMResult InterpretGenerateResponse(UnityWebRequest webRequest, string url, float elapsedSeconds)
        {
            string body = webRequest.downloadHandler?.text;

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.Success:
                    break;

                case UnityWebRequest.Result.ConnectionError:
                    if (!string.IsNullOrEmpty(webRequest.error) &&
                        webRequest.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return LLMResult.Fail(LLMErrorType.Timeout, $"Zaman aşımı ({requestTimeoutSeconds} sn).");
                    }

                    return LLMResult.Fail(LLMErrorType.ConnectionFailed,
                        $"Ollama sunucusuna bağlanılamadı ({url}). Ayrıntı: {webRequest.error}");

                case UnityWebRequest.Result.ProtocolError:
                    string serverError = TryReadServerError(body);
                    string hint = webRequest.responseCode == 404 ? $" (Model yüklü mü? → ollama pull {model})" : string.Empty;
                    return LLMResult.Fail(LLMErrorType.HttpError,
                        $"HTTP {webRequest.responseCode}: {serverError ?? webRequest.error}{hint}");

                case UnityWebRequest.Result.DataProcessingError:
                    return LLMResult.Fail(LLMErrorType.InvalidResponse, $"Veri işleme hatası: {webRequest.error}");

                default:
                    return LLMResult.Fail(LLMErrorType.Unknown, $"Beklenmeyen istek durumu: {webRequest.result}");
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                return LLMResult.Fail(LLMErrorType.EmptyResponse, "Sunucu boş gövde döndürdü.");
            }

            OllamaGenerateResponse response;
            try
            {
                response = JsonConvert.DeserializeObject<OllamaGenerateResponse>(body);
            }
            catch (JsonException exception)
            {
                return LLMResult.Fail(LLMErrorType.InvalidResponse, $"Ollama yanıtı ayrıştırılamadı: {exception.Message}");
            }

            if (response == null || string.IsNullOrWhiteSpace(response.Response))
            {
                return LLMResult.Fail(LLMErrorType.EmptyResponse, "Model boş yanıt üretti.");
            }

            if (response.DoneReason == "length")
            {
                Debug.LogWarning("[Ollama] Yanıt token sınırında kesildi; JSON eksik olabilir (maxTokens'ı artırın).", this);
            }

            if (logTraffic)
            {
                Debug.Log($"[Ollama] ← {elapsedSeconds:0.00} sn, {response.EvalCount} token\n{response.Response}", this);
            }

            return LLMResult.Ok(response.Response);
        }

        private static string TryReadServerError(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<OllamaErrorResponse>(body)?.Error;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Yardımcılar

        private LLMResult BuildCancellationResult(CancellationToken callerToken)
        {
            if (callerToken.IsCancellationRequested || (_lifetimeCts != null && _lifetimeCts.IsCancellationRequested))
            {
                return LLMResult.Fail(LLMErrorType.Cancelled, "İstek iptal edildi.");
            }

            return LLMResult.Fail(LLMErrorType.Timeout, $"Ollama {requestTimeoutSeconds} sn içinde yanıt vermedi.");
        }

        private CancellationTokenSource CreateLinkedSource(CancellationToken callerToken, CancellationToken timeoutToken)
        {
            CancellationToken lifetimeToken = _lifetimeCts?.Token ?? CancellationToken.None;
            return CancellationTokenSource.CreateLinkedTokenSource(callerToken, timeoutToken, lifetimeToken);
        }

        private static string CombineUrl(string root, string path)
        {
            return $"{root.TrimEnd('/')}/{path.TrimStart('/')}";
        }

        private static bool IsSameModel(string installed, string configured)
        {
            if (string.IsNullOrEmpty(installed) || string.IsNullOrEmpty(configured))
            {
                return false;
            }

            return string.Equals(installed, configured, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(installed, configured + ":latest", StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
