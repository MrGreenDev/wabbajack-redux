using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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

        public ValueTask<T?> DeserializeAsync<T>(Stream stream)
        {
            return JsonSerializer.DeserializeAsync<T>(stream, Options);
        }

        public string Serialize<T>(T data)
        {
            return JsonSerializer.Serialize(data, Options);
        }
    }
}