using FreeRP.Database;
using FreeRP.FrpServices;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FreeRP.Helpers.Database
{
    public ref struct FrpProcessJsonToRead(FrpDatabase db, FrpDataset dataSet, FrpDatabasePermissions ac)
    {
        private readonly FrpDatabase _db = db;
        private readonly FrpDataset _dataset = dataSet;
        private readonly FrpDatabasePermissions _ac = ac;

        private readonly CultureInfo _ci = CultureInfo.GetCultureInfo("en-US");
        private readonly StringBuilder _sb = new();
        private Utf8JsonReader _reader;
        private bool _anyAllowed = false;

        public static ValueTask<string?> GetJsonAsync(
            FrpDatabase db, FrpDataset dataSet, FrpDatabasePermissions ac, string json)
            => GetJsonAsync(db, dataSet, ac, Json.GetJsonAsSpan(json));

        public static ValueTask<string?> GetJsonAsync(FrpDatabase db, FrpDataset dataSet, FrpDatabasePermissions ac, ReadOnlySpan<byte> json)
        {
            FrpProcessJsonToRead pjta = new(db, dataSet, ac);
            return ValueTask.FromResult(pjta.GetJson(json));
        }

        public string? GetJson(string json) => GetJson(Json.GetJsonAsSpan(json));

        public string? GetJson(ReadOnlySpan<byte> json)
        {
            _reader = new(json);

            ReadValue($"{IFrpDatabaseService.UriSchemeDatabase}://{_db.DatabaseId}/{_dataset.DatasetId}");

            DeleteLastComa();

            if (_anyAllowed)
                return _sb.ToString();

            return null;
        }

        private void ReadValue(string parentPath) 
        {
            _reader.Read();
            switch (_reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    ReadObject(parentPath);
                    break;
                case JsonTokenType.StartArray:
                    ReadArray(parentPath);
                    break;
                case JsonTokenType.PropertyName:
                    ReadPropertyName(parentPath);
                    break;
                case JsonTokenType.String:
                    _sb.Append($"\"{_reader.GetString()}\",");
                    break;
                case JsonTokenType.Number:
                    _sb.Append($"{_reader.GetDecimal().ToString(_ci)},");
                    break;
                case JsonTokenType.True:
                    _sb.Append("true,");
                    break;
                case JsonTokenType.False:
                    _sb.Append("false,");
                    break;
                case JsonTokenType.Null:
                    _sb.Append("null,");
                    break;
                default:
                    break;
            }
        }

        private void ReadObject(string parentPath)
        {
            _sb.Append('{');
            while (_reader.TokenType is not JsonTokenType.EndObject)
            {
                ReadValue(parentPath);
            }

            DeleteLastComa();

            _sb.Append("},");
        }

        private void ReadPropertyName(string parentPath)
        {
            if (_reader.GetString() is string s && s.Length > 0)
            {
                var p = Json.ToCamelCase(s);
                string path = $"{parentPath}/{p}";

                if (_ac.All.TryGetValue(path, out var val))
                {
                    if (val.IsPrimaryKey)
                    {
                        _sb.Append($"\"{p}\":");
                        ReadValue(path);
                    }
                    else if (val.PermissionValues.Read == FrpPermissionValue.Allow)
                    {
                        _anyAllowed = true;
                        _sb.Append($"\"{p}\":");
                        ReadValue(path);
                    }
                    else
                    {
                        _reader.Skip();
                    }
                }
                else
                {
                    _anyAllowed = true;
                    _sb.Append($"\"{p}\":");
                    ReadValue(path);
                }
            }
        }

        private void ReadArray(string parentPath)
        {
            var p = _ac.All[parentPath];
            string path = parentPath;
            if (p.Children.Count > 0)
                path = p.Children[0].Uri;

            _sb.Append('[');
            while (_reader.TokenType is not JsonTokenType.EndArray)
            {
                ReadValue(path);
            }

            DeleteLastComa();

            _sb.Append("],");
        }

        private readonly void DeleteLastComa()
        {
            if (_sb[^1] == ',')
                _sb.Length--;
        }
    }
}
