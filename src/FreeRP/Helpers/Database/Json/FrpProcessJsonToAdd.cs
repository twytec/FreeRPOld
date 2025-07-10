using FreeRP.Database;
using FreeRP.FrpServices;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace FreeRP.Helpers.Database
{
    public ref struct FrpProcessJsonToAdd(FrpDatabase db, FrpDataset dataSet, FrpDatabasePermissions ac)
    {
        private readonly FrpDatabase _db = db;
        private readonly FrpDataset _dataSet = dataSet;
        private readonly FrpDatabasePermissions _ac = ac;

        private string _id = string.Empty;

        private readonly CultureInfo _ci = CultureInfo.GetCultureInfo("en-US");
        private readonly StringBuilder _sb = new();
        private Utf8JsonReader _reader;
        private bool _anyAllowed = false;

        #region Add

        public static ValueTask<string?> GetJsonAsync(
            FrpDatabase db, FrpDataset dataSet, FrpDatabasePermissions ac, string id, string json)
            => GetJsonAsync(db, dataSet, ac, id, Json.GetJsonAsSpan(json));

        public static ValueTask<string?> GetJsonAsync(FrpDatabase db, FrpDataset dataSet, FrpDatabasePermissions ac, string id, ReadOnlySpan<byte> json)
        {
            FrpProcessJsonToAdd pjta = new(db, dataSet, ac);
            return ValueTask.FromResult(pjta.GetJson(id, json));
        }

        public string? GetJson(string id, string json) => GetJson(id, Json.GetJsonAsSpan(json));

        public string? GetJson(string id, ReadOnlySpan<byte> json)
        {
            _id = id;
            _reader = new(json);

            ReadValue($"{IFrpDatabaseService.UriSchemeDatabase}://{_db.DatabaseId}/{_dataSet.DatasetId}");

            DeleteLastComa();

            if (_anyAllowed)
                return _sb.ToString();

            return null;
        }

        #endregion

        private void ReadObject(string parentPath)
        {
            var p = _ac.All[parentPath];
            if (p.DataType is FrpDatabaseDataType.FieldObject)
            {
                _sb.Append('{');
                while (_reader.TokenType is not JsonTokenType.EndObject)
                {
                    ReadValue(parentPath);
                }

                DeleteLastComa();

                _sb.Append("},");
            }
            else
            {
                while (_reader.TokenType is not JsonTokenType.EndObject)
                    _reader.Read();

                SetDefault(p.DataType);
            }
        }

        private void ReadArray(string parentPath)
        {
            var p = _ac.All[parentPath];
            if (p.DataType is FrpDatabaseDataType.FieldArray)
            {
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
            else
            {
                while (_reader.TokenType is not JsonTokenType.EndArray)
                    _reader.Read();

                SetDefault(p.DataType);
            }

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
                        _sb.Append($"\"{p}\":\"{_id}\",");
                        _reader.Skip();
                    }
                    else if (val.PermissionValues.Add == FrpPermissionValue.Allow)
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
                else if (_dataSet.AllowUnknownFields)
                {
                    FrpException.Error(FrpErrorType.ErrorDatasetDataInvalid, "");
                }
                else
                {
                    _reader.Skip();
                }
            }
        }

        private void ReadValue(string parentPath)
        {
            var p = _ac.All[parentPath];

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
                case JsonTokenType.Comment:
                    break;
                case JsonTokenType.String:
                    if (p.DataType is FrpDatabaseDataType.FieldString)
                        _sb.Append($"\"{_reader.GetString()}\",");
                    else
                        SetDefault(p.DataType);
                    break;
                case JsonTokenType.Number:
                    if (p.DataType is FrpDatabaseDataType.FieldNumber)
                        _sb.Append($"{_reader.GetDecimal().ToString(_ci)},");
                    else
                        SetDefault(p.DataType);
                    break;
                case JsonTokenType.True:
                    if (p.DataType is FrpDatabaseDataType.FieldBoolean)
                        _sb.Append("true,");
                    else
                        SetDefault(p.DataType);
                    break;
                case JsonTokenType.False:
                    if (p.DataType is FrpDatabaseDataType.FieldBoolean)
                        _sb.Append("false,");
                    else
                        SetDefault(p.DataType);
                    break;
                case JsonTokenType.Null:
                    if (p.DataType is FrpDatabaseDataType.FieldNull)
                        _sb.Append("null,");
                    else
                        SetDefault(p.DataType);
                    break;
                default:
                    break;
            }
        }

        private readonly void DeleteLastComa()
        {
            if (_sb[^1] == ',')
                _sb.Length--;
        }

        private readonly void SetDefault(FrpDatabaseDataType dataType)
        {
            switch (dataType)
            {
                case FrpDatabaseDataType.FieldNull:
                    _sb.Append("null,");
                    break;
                case FrpDatabaseDataType.FieldString:
                    _sb.Append("\"\",");
                    break;
                case FrpDatabaseDataType.FieldNumber:
                    _sb.Append("0,");
                    break;
                case FrpDatabaseDataType.FieldArray:
                    _sb.Append("[],");
                    break;
                case FrpDatabaseDataType.FieldBoolean:
                    _sb.Append("false,");
                    break;
                case FrpDatabaseDataType.FieldObject:
                    _sb.Append("{},");
                    break;
                default:
                    break;
            }
        }
    }
}
