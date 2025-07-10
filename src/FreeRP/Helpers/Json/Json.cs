using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FreeRP.Helpers
{
    public static class Json
    {
        private static JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static readonly JsonSerializerOptions optionsIndented = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

        static Json()
        {
            options.AddProtobufSupport();
            optionsIndented.AddProtobufSupport();
        }

        public static string GetJson(object model) => JsonSerializer.Serialize(model, options);
        public static string GetJsonIndented(object model) => JsonSerializer.Serialize(model, optionsIndented);
        public static T? GetModel<T>(string json) => JsonSerializer.Deserialize<T>(json, options);
        public static string ToCamelCase(string s) => JsonNamingPolicy.CamelCase.ConvertName(s);

        public static bool TryGetModel<T>(string json, [MaybeNullWhen(false)] out T data)
        {
            try
            {
                data = JsonSerializer.Deserialize<T>(json, options);
                if (data is not null)
                    return true;

            }
            catch (Exception)
            {
            }

            data = default;
            return false;
        }

        public static bool TryGetJsonObject(ReadOnlySpan<byte> json, [MaybeNullWhen(false)] out JsonObject data)
        {
            try
            {
                var jn = JsonNode.Parse(json);
                if (jn is not null)
                {
                    data = jn.AsObject();
                    return true;
                }
            }
            catch (Exception)
            {
            }

            data = default;
            return false;
        }

        public static bool TryGetJsonObject(string json, [MaybeNullWhen(false)] out JsonObject data)
        {
            try
            {
                var jn = JsonNode.Parse(json);
                if (jn is not null)
                {
                    data = jn.AsObject();
                    return true;
                }
            }
            catch (Exception)
            {
            }

            data = default;
            return false;
        }

        public static ReadOnlySpan<byte> GetJsonAsSpan(string json)
        {
            var jsonChars = json.AsSpan();
            var byteCount = Encoding.UTF8.GetByteCount(jsonChars);
            var buffer = ArrayPool<byte>.Shared.Rent(byteCount);
            var length = Encoding.UTF8.GetBytes(jsonChars, buffer);
            return buffer.AsSpan()[..length];
        }
    }
}
