using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FreeRP.Helpers
{
    public class GrpcJson
    {
        private static JsonFormatter.Settings _sfs = JsonFormatter.Settings.Default.WithFormatEnumsAsIntegers(true);
        private static JsonParser.Settings _sps = JsonParser.Settings.Default
            .WithIgnoreUnknownFields(true);

        private static readonly JsonFormatter _jf = new(_sfs);
        private static readonly JsonParser _jp = new(_sps);
        public static T? GetModel<T>(string? json) where T : IMessage, new()
        {
            if (json is null)
            {
                return default;
            }

            return _jp.Parse<T>(json);
        }

        public static bool TryGetModel<T>(string json, [MaybeNullWhen(false)] out T data) where T : IMessage, new()
        {
            try
            {
                data = _jp.Parse<T>(json);
                if (data is not null)
                    return true;

            }
            catch (Exception)
            {
            }

            data = default;
            return false;
        }

        public static string GetJson(IMessage model) => _jf.Format(model);
    }
}
