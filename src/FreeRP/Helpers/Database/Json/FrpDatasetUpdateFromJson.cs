using FreeRP.Database;
using FreeRP.FrpServices;
using System.Buffers;
using System.Data;
using System.Text;
using System.Text.Json;

namespace FreeRP.Helpers.Database
{
    public ref struct FrpDatasetUpdateFromJson
    {
        private readonly ReadOnlySpan<byte> _json;
        private Utf8JsonReader _reader;
        private readonly FrpDataset _dataSet;
        private bool _findId = false;

        public static ValueTask UpdateAsync(string json, FrpDataset dataSet)
            => UpdateAsync(Json.GetJsonAsSpan(json), dataSet);

        public static ValueTask UpdateAsync(ReadOnlySpan<byte> json, FrpDataset dataSet)
        {
            _ = new FrpDatasetUpdateFromJson(json, dataSet);
            return ValueTask.CompletedTask;
        }

        public FrpDatasetUpdateFromJson(string json, FrpDataset dataSet) : this(Json.GetJsonAsSpan(json), dataSet) { }

        public FrpDatasetUpdateFromJson(ReadOnlySpan<byte> json, FrpDataset dataSet)
        {
            _json = json;
            _dataSet = dataSet;
            _reader = new Utf8JsonReader(_json);
            ReadValue(null);

            if (_findId == false)
            {
                FrpException.Error(FrpErrorType.ErrorDatasetPrimaryKeyRequired, "");
            }
        }

        private void ReadValue(FrpDataField? field)
        {
            _reader.Read();
            switch (_reader.TokenType)
            {
                case JsonTokenType.StartObject:
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
            if (_reader.GetString() is string s && s.Length > 0)
            {
                string fieldId = Json.ToCamelCase(s);
                if (_dataSet.Fields.FirstOrDefault(x => x.FieldId == fieldId) is FrpDataField a)
                {
                    if (a.IsPrimaryKey)
                    {
                        _findId = true;
                        _reader.Skip();
                    }
                    else
                    {
                        ReadValue(a);
                    }
                }
                else if (field is not null && field.Fields.FirstOrDefault(x => x.FieldId == fieldId) is FrpDataField b)
                {
                    ReadValue(b);
                }
                else 
                {
                    if (field is null)
                    {
                        field = new() { FieldId = fieldId };
                        ReadValue(field);
                        _dataSet.Fields.Add(field);
                    }
                    else if (field.DataType == FrpDatabaseDataType.FieldObject)
                    {
                        FrpDataField f = new() { FieldId = fieldId };
                        ReadValue(f);

                        if (field.Fields.FirstOrDefault(x => x.FieldId == f.FieldId) is FrpDataField ff)
                            ff.DataType = f.DataType;
                        else
                            field.Fields.Add(f);
                    }
                }
            }
        }
    }
}
