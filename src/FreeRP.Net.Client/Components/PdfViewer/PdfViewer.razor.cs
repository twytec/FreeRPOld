using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Client.Components
{
    public sealed partial class PdfViewer : IAsyncDisposable
    {
        private const string JAVASCRIPT_FILE = "./_content/FreeRP.Net.Client/Components/PdfViewer/PdfViewer.razor.js";

        [Parameter]
        public string? FromBase64 { get; set; }

        [Parameter]
        public string? FromUrl { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;
        private IJSObjectReference? Module { get; set; }
        private readonly string Id = Guid.NewGuid().ToString();

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                if (FromBase64 is not null)
                {
                    PdfLoadFromBase64(FromBase64);
                }
                else if (FromUrl is not null)
                {
                    PdfLoadFromUrl(FromUrl);
                }
            }
        }

        public async void PdfLoadFromBase64(string b64)
        {
            Module ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", JAVASCRIPT_FILE);
            await Module.InvokeVoidAsync("loadPdf", Id, b64, "");
        }

        public async void PdfLoadFromUrl(string url)
        {
            try
            {
                Module ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", JAVASCRIPT_FILE);
                await Module.InvokeVoidAsync("loadPdf", Id, "", url);
            }
            catch (Exception ex)
            {
                var m = ex;
            }
            
        }

        public async ValueTask DisposeAsync()
        {
            if (Module is not null)
            {
                await Module.DisposeAsync();
            }
        }
    }
}
