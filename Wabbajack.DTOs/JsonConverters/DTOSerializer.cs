using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wabbajack.DTOs.JsonConverters
{
    public class DTOSerializer
    {
        private readonly JsonSerializerOptions _options;

        public DTOSerializer(IEnumerable<JsonConverter> converters)
        {
            _options = new JsonSerializerOptions();
            foreach (var c in converters) _options.Converters.Add(c);
        }

        public T? Deserialize<T>(string text)
        {
            return JsonSerializer.Deserialize<T>(text, _options);
        }

        public string Serialize<T>(T data)
        {
            return JsonSerializer.Serialize(data, _options);
        }
    }
}