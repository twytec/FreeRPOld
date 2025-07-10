using FreeRP.Net.Client.Data;
using FreeRP.Net.Client.Translation;
using Microsoft.JSInterop;

namespace FreeRP.Net.Client.Blazor.Data
{
    public class FrpEnvironment
    {
        private readonly IJSRuntime _js;
        private readonly HttpClient _httpClient;
        private AppSettings? _appConfig;

        public FrpEnvironment(IJSRuntime js, HttpClient httpClient)
        {
            _js = js;
            _httpClient = httpClient;
        }

        public async Task<string> GetPreferencesAsync(string key)
        {
            return await _js.InvokeAsync<string>("getStorage", key);
        }

        public async Task SetPreferencesAsync(string key, string value)
        {
            await _js.InvokeVoidAsync("setStorage", key, value);
        }

        public async Task<string> GetUserLanguageAsync()
        {
            return await _js.InvokeAsync<string>("getUserLang");
        }

        public async Task<AppSettings> GetAppConfigAsync()
        {
            if (_appConfig == null)
            {
                try
                {
                    var res = await _httpClient.GetStringAsync("/appconfig.json");
                    if (res != null)
                    {
                        var ac = Helpers.Json.GetModel<AppSettings>(res);
                        if (ac != null)
                        {
                            _appConfig = ac;
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            if (_appConfig == null)
            {
                _appConfig = new();
            }

            return _appConfig;
        }

        public async Task<I18n> GetTranslationAsync(string code)
        {
            var res = await _httpClient.GetStringAsync($"/i18n/{code}.json");
            if (res != null && Helpers.Json.GetModel<I18n>(res) is I18n i18n)
            {
                return i18n;
            }

            return new();
        }

        public async Task CopyToClipboard(string source)
        {
            await _js.InvokeVoidAsync("toClipboard", source);
        }
    }
}
