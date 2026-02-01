using System.Text.Json.Serialization;

namespace NDTBundlePOC.Core.Models
{
    public class PrintModeRequest
    {
        [JsonPropertyName("testMode")]
        public bool TestMode { get; set; }
    }
}

