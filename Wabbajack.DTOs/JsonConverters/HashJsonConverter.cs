using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Interfaces;

namespace Wabbajack.DTOs.JsonConverters
{
    public class HashJsonConverter : JsonConverter<Hash>, ISingletonService 
    {
        public override Hash Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Hash.FromBase64(reader.ValueSpan);
        }

        public override void Write(Utf8JsonWriter writer, Hash value, JsonSerializerOptions options)
        {
            Span<byte> data = stackalloc byte[12];
            value.ToBase64(data);
            writer.WriteStringValue(data);
        }
    }
}