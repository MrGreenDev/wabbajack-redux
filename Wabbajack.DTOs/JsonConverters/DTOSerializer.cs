using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wabbajack.DTOs.JsonConverters
{
    public class DTOSerializer
    {
        public readonly JsonSerializerOptions Options;

        public DTOSerializer(IEnumerable<JsonConverter> converters)
        {
            Options = new JsonSerializerOptions();
            foreach (var c in converters) Options.Converters.Add(c);
        }

        public T? Deserialize<T>(string text)
        {
            return JsonSerializer.Deserialize<T>(text, Options);
        }

        public string Serialize<T>(T data)
        {
            return JsonSerializer.Serialize(data, Options);
        }
    }
}
