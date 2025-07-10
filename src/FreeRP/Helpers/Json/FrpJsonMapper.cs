using FreeRP.Database;
using System.Data;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FreeRP.Helpers
{
    public ref struct FrpJsonMapper
    {
        public record JsonMapperValue(string? Val, FrpDatabaseDataType DataType);
        private readonly Dictionary<string, JsonMapperValue> _dict = [];

        private Utf8JsonReader _reader;
        private readonly CultureInfo _ci = CultureInfo.GetCultureInfo("en-US");

        public FrpJsonMapper(string json, string path = "")
        {
            _reader = new(Json.GetJsonAsSpan(json));
            _ = ReadValue(path);
        }

        public readonly JsonMapperValue? GetValue(string key)
        {
            if (_dict.TryGetValue(key, out var val))
            {
                return val;
            }

            return null;
        }

        private string ReadValue(string parentPath)
        {
            _reader.Read();
            switch (_reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    var ov = ReadObject(parentPath);
                    _dict[parentPath] = new JsonMapperValue(ov, FrpDatabaseDataType.FieldObject);
                    return $"{ov},";
                case JsonTokenType.StartArray:
                    var av = ReadArray(parentPath);
                    _dict[parentPath] = new JsonMapperValue(av, FrpDatabaseDataType.FieldArray);
                    return $"{av},";
                case JsonTokenType.PropertyName:
                    var pn = ReadPropertyName(parentPath);
                    string path = $"{parentPath}/{pn}";
                    var val = ReadValue(path);
                    return $"\"{pn}\":{val}";
                case JsonTokenType.String:
                    var sv = _reader.GetString();
                    _dict[parentPath] = new JsonMapperValue(sv, FrpDatabaseDataType.FieldString);
                    return $"\"{sv}\",";
                case JsonTokenType.Number:
                    string nv = _reader.GetDecimal().ToString(_ci);
                    _dict[parentPath] = new JsonMapperValue(nv, FrpDatabaseDataType.FieldNumber);
                    return $"{nv},";
                case JsonTokenType.True:
                    string tv = "true";
                    _dict[parentPath] = new JsonMapperValue(tv, FrpDatabaseDataType.FieldBoolean);
                    return $"{tv},";
                case JsonTokenType.False:
                    string fv = "false";
                    _dict[parentPath] = new JsonMapperValue(fv, FrpDatabaseDataType.FieldBoolean);
                    return $"{fv},";
                case JsonTokenType.Null:
                    string nullVal = "null" ;
                    _dict[parentPath] = new JsonMapperValue(nullVal, FrpDatabaseDataType.FieldNull);
                    return $"{nullVal},";
            }

            return string.Empty;
        }

        private string ReadObject(string parentPath)
        {
            StringBuilder sb = new();
            sb.Append('{');
            while (_reader.TokenType is not JsonTokenType.EndObject)
            {
                var s = ReadValue(parentPath);
                sb.Append(s);
            }

            DeleteLastComa(sb);

            sb.Append('}');
            return sb.ToString();
        }

        private string ReadArray(string parentPath)
        {
            StringBuilder sb = new();
            sb.Append('[');

            while (_reader.TokenType is not JsonTokenType.EndArray)
            {
                var val = ReadValue(parentPath);
                sb.Append(val);
            }

            DeleteLastComa(sb);

            sb.Append(']');
            return sb.ToString();
        }

        private string ReadPropertyName(string parentPath)
        {
            if (_reader.GetString() is string s && s.Length > 0)
            {
                return Json.ToCamelCase(s);
            }

            return string.Empty;
        }

        private static void DeleteLastComa(StringBuilder sb)
        {
            if (sb[^1] == ',')
                sb.Length--;
        }
    }
}
