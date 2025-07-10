using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Net.Http;
using System.Collections.Concurrent;

namespace FreeRP.Localization
{
    public class FrpLocalizationService
    {
        private static readonly ConcurrentDictionary<string, FrpLocalization> _lang = [];

        public static string[] GetSupportedLanguages()
        {
            HashSet<string> res = new(DefaultLanguages);
            foreach (var l in _lang)
            {
                res.Add(l.Key);
            }
            return res.ToArray();
        }

        public string CurrentLanguage { get; set; } = string.Empty;
        public FrpLocalization Text { get; set; } = new();
        
        public delegate void FrpLocalizationEventHandler();
        public event FrpLocalizationEventHandler? TextChanged;
        public static readonly string[] DefaultLanguages = ["en", "de"];

        public FrpLocalizationService() 
        {
            SetText(Thread.CurrentThread.CurrentCulture.Name); 
        }

        public void SetText(string code)
        {
            string c;
            if (_lang.TryGetValue(code, out FrpLocalization? value))
            {
                Text = value;
                CurrentLanguage = code;
                return;
            }
            else if (DefaultLanguages.Contains(code))
            {
                c = code;
            }
            else if (code.Contains('-') && code.Split('-') is string[] s && s.Length > 0 && DefaultLanguages.Contains(s[0]))
            {
                c = s[0];
            }
            else
                return;

            CurrentLanguage = c;
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"FreeRP.Localization.Lang.{c}.json";
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is not null)
            {
                using StreamReader reader = new(stream);
                var json = reader.ReadToEnd();
                var txt = Helpers.Json.GetModel<FrpLocalization>(json);
                if (txt is not null)
                {
                    _lang[c] = txt;
                    Text = txt;
                    TextChanged?.Invoke();
                }
            }
        }
    }
}
