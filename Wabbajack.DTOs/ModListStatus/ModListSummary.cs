using System;
using System.Text.Json.Serialization;

namespace Wabbajack.DTOs
{
    public class ModListSummary
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("machineURL")]
        public string MachineURL { get; set; } = string.Empty;

        [JsonPropertyName("checked")]
        public DateTime Checked { get; set; }
        [JsonPropertyName("failed")]
        public int Failed { get; set; }
        [JsonPropertyName("passed")]
        public int Passed { get; set; }
        [JsonPropertyName("updating")]
        public int Updating { get; set; }

        [JsonPropertyName("mirrored")]
        public int Mirrored { get; set; }

        [JsonPropertyName("link")]
        public string Link => $"/lists/status/{MachineURL}.json";
        [JsonPropertyName("report")]
        public string Report => $"/lists/status/{MachineURL}.html";
        
        [JsonPropertyName("modlist_missing")]
        public bool ModListIsMissing { get; set; }
        
        [JsonPropertyName("has_failures")]
        public bool HasFailures => Failed > 0 || ModListIsMissing;
    }
}