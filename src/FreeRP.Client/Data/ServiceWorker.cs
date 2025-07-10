using Microsoft.JSInterop;

namespace FreeRP.Client.Data
{
    public class ServiceWorker
    {
        [JSInvokable]
        public static void MessageFromServiceWorker(string msg)
        {

        }

        public static async Task MessageToServiceWorkerAsync(IJSRuntime js, string msg) 
        {
            await js.InvokeVoidAsync("messageToServiceWorker", msg);
        }
    }
}
