using Microsoft.JSInterop;

namespace FreeRP.Client.Data
{
    public class LocalStorage(IJSRuntime js)
    {
        private readonly IJSRuntime _js = js;

        public async Task<string?> GetPreferencesAsync(string key)
        {
            return await _js.InvokeAsync<string?>("getStorage", key);
        }

        public async Task SetPreferencesAsync(string key, string value)
        {
            await _js.InvokeVoidAsync("setStorage", key, value);
        }
    }
}
