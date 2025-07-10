using FreeRP.Database;
using FreeRP.FrpServices;
using System.Text.Json;

namespace FreeRP.Helpers.Database
{
    public ref struct FrpDatasetFromJson
    {
        private readonly ReadOnlySpan<byte> _json;
        private Utf8JsonReader _reader;
        private FrpDataset? _dataset;
        private bool _findId = false;

        /// <summary>
        /// Returns <see cref="FrpDataset"/> from JSON async
        /// </summary>
        /// <param name="json"></param>
        /// <param name="datasetId"></param>
        /// <returns></returns>
        public static Task<FrpDataset> GetDatasetAsync(string json, string datasetId)
        {
            var js = Json.GetJsonAsSpan(json);
            return GetDatasetAsync(js, datasetId);
        }

        /// <summary>
        /// Returns <see cref="FrpDataset"/> from JSON async
        /// </summary>
        /// <param name="json"></param>
        /// <param name="datasetId"></param>
        /// <returns></returns>
        public static Task<FrpDataset> GetDatasetAsync(ReadOnlySpan<byte> json, string datasetId)
        {
            FrpDatasetFromJson jdt = new(json);
            return Task.FromResult(jdt.GetDataset(datasetId));
        }

        public FrpDatasetFromJson(string json) : this(Json.GetJsonAsSpan(json)) { }

        public FrpDatasetFromJson(ReadOnlySpan<byte> json)
        {
            _json = json;
            _reader = new Utf8JsonReader(_json);
        }

        /// <summary>
        /// Returns <see cref="FrpDataset"/> from JSON
        /// </summary>
        /// <param name="datasetId"></param>
        /// <returns></returns>
        public FrpDataset GetDataset(string datasetId)
        {
            _dataset = new() { DatasetId = datasetId };
            ReadValue(null);

            if (_findId == false)
            {
                FrpException.Error(FrpErrorType.ErrorDatasetPrimaryKeyRequired, "");
            }

            return _dataset;
        }

        private void ReadObject(FrpDataField? field)
        {
            while (_reader.TokenType is not JsonTokenType.EndObject)
            {
                ReadValue(field);
            }
        }

        private void ReadArray(FrpDataField? field)
        {
            FrpDataField arrField = new();
            if (field is not null)
                arrField.FieldId = field.FieldId;
            else
                arrField.FieldId = "a";

            ReadValue(arrField);

            if (field is not null && arrField.DataType is not FrpDatabaseDataType.FieldNull)
                field.Fields.Add(arrField);

            while (_reader.TokenType is not JsonTokenType.EndArray)
            {
                _reader.Read();
            }
        }

        private void ReadPropertyName(FrpDataField? field)
        {
            if (_dataset is not null && _reader.GetString() is string s && s.Length > 0)
            {
                if (field is null)
                {
                    field = new()
                    {
                        FieldId = Json.ToCamelCase(s)
                    };

                    ReadValue(field);

                    if (field.FieldId == IFrpDatabaseService.DatabasePrimaryKeyName)
                    {
                        _findId = true;
                        field.IsPrimaryKey = true;
                        field.DataType = FrpDatabaseDataType.FieldString;
                    }

                    if (_dataset.Fields.FirstOrDefault(x => x.FieldId == field.FieldId) is FrpDataField f)
                        throw new JsonException($"{f.FieldId} is already included.");
                    else
                        _dataset.Fields.Add(field);
                }
                else
                {
                    FrpDataField f = new()
                    {
                        FieldId = Json.ToCamelCase(s)
                    };
                    ReadValue(f);

                    if (field.Fields.FirstOrDefault(x => x.FieldId == f.FieldId) is FrpDataField ff)
                        throw new JsonException($"{f.FieldId} is already included.");
                    else
                        field.Fields.Add(f);
                }
            }
        }

        private void ReadValue(FrpDataField? field)
        {
            _reader.Read();
            switch (_reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    if (field is not null)
                        field.DataType = FrpDatabaseDataType.FieldObject;

                    ReadObject(field);
                    break;
                case JsonTokenType.PropertyName:
                    ReadPropertyName(field);
                    break;
                case JsonTokenType.StartArray:
                    if (field is not null)
                        field.DataType = FrpDatabaseDataType.FieldArray;

                    ReadArray(field);
                    break;
                case JsonTokenType.String:
                    if (field is not null)
                        field.DataType = FrpDatabaseDataType.FieldString;
                    break;
                case JsonTokenType.Number:
                    if (field is not null)
                        field.DataType = FrpDatabaseDataType.FieldNumber;
                    break;
                case JsonTokenType.True:
                case JsonTokenType.False:
                    if (field is not null)
                        field.DataType = FrpDatabaseDataType.FieldBoolean;
                    break;
                case JsonTokenType.Null:
                    if (field is not null)
                        field.DataType = FrpDatabaseDataType.FieldNull;
                    break;
                default:
                    break;
            }
        }
    }
}
