using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AdventureGame
{
    // =====================================================================
    //  Ollama REST API veri transfer nesneleri (DTO)
    //  Doküman: https://github.com/ollama/ollama/blob/main/docs/api.md
    // =====================================================================

    /// <summary>POST /api/generate istek gövdesi.</summary>
    [Serializable]
    public sealed class OllamaGenerateRequest
    {
        [JsonProperty("model")] public string Model;

        [JsonProperty("prompt")] public string Prompt;

        [JsonProperty("system", NullValueHandling = NullValueHandling.Ignore)]
        public string SystemPrompt;

        /// <summary>false → tek parça yanıt (akış yok). Ayrıştırmayı basitleştirir.</summary>
        [JsonProperty("stream")] public bool Stream;

        /// <summary>
        /// YAPILANDIRILMIŞ ÇIKTI: Buraya JSON Schema nesnesi verilince Ollama, modelin
        /// çıktısını gramer kısıtlamasıyla bu şemaya uymaya ZORLAR (Ollama v0.5+).
        /// "json" stringi verilirse sadece geçerli JSON garanti edilir (şema değil).
        /// </summary>
        [JsonProperty("format", NullValueHandling = NullValueHandling.Ignore)]
        public JToken Format;

        /// <summary>Modelin bellekte kalma süresi (örn. "10m"). İlk yüklemeyi tekrarlamamak için.</summary>
        [JsonProperty("keep_alive", NullValueHandling = NullValueHandling.Ignore)]
        public string KeepAlive;

        [JsonProperty("options", NullValueHandling = NullValueHandling.Ignore)]
        public OllamaOptions Options;
    }

    [Serializable]
    public sealed class OllamaOptions
    {
        [JsonProperty("temperature")] public float Temperature = 0.6f;

        /// <summary>Üretilecek maksimum token. Sonsuz döngüye/uzun beklemeye karşı sınır.</summary>
        [JsonProperty("num_predict")] public int NumPredict = 300;
    }

    /// <summary>POST /api/generate yanıtı (stream: false).</summary>
    [Serializable]
    public sealed class OllamaGenerateResponse
    {
        [JsonProperty("model")] public string Model;
        [JsonProperty("created_at")] public string CreatedAt;

        /// <summary>Modelin ürettiği metin. Şema kullanıldığında bu alan bir JSON METNİDİR (string içinde JSON).</summary>
        [JsonProperty("response")] public string Response;

        [JsonProperty("done")] public bool Done;
        [JsonProperty("done_reason")] public string DoneReason;
        [JsonProperty("total_duration")] public long TotalDurationNs;
        [JsonProperty("eval_count")] public int EvalCount;
    }

    /// <summary>Ollama hata gövdesi: {"error": "model 'x' not found"}</summary>
    [Serializable]
    public sealed class OllamaErrorResponse
    {
        [JsonProperty("error")] public string Error;
    }

    /// <summary>GET /api/tags yanıtı (yüklü modeller listesi) – sağlık kontrolü için.</summary>
    [Serializable]
    public sealed class OllamaTagsResponse
    {
        [JsonProperty("models")] public List<OllamaModelInfo> Models;
    }

    [Serializable]
    public sealed class OllamaModelInfo
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("model")] public string Model;
    }
}
