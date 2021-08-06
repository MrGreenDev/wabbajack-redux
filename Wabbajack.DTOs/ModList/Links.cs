using System.Text.Json.Serialization;
using Wabbajack.DTOs.JsonConverters;

namespace Wabbajack.DTOs
{
    [JsonName("Links")]
    public class LinksObject
    {
        [JsonPropertyName("image")]
        public string ImageUri { get; set; } = string.Empty;

        [JsonPropertyName("readme")]
        public string Readme { get; set; } = string.Empty;

        [JsonPropertyName("download")]
        public string Download { get; set; } = string.Empty;

        [JsonPropertyName("machineURL")]
        public string MachineURL { get; set; } = string.Empty;
    }
}