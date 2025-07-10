using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Client.GrpcServices
{
    public class PdfService(IJSRuntime js)
    {
        private IJSObjectReference? _pdfHelperJs;
        private readonly IJSRuntime _js = js;

        /// <summary>
        /// Convert PDF to images with text. <see cref="Data.PdfPage"/>
        /// </summary>
        /// <param name="pdfAsBase64">PDF as Base64String</param>
        /// <param name="scale">Scale factor. 1 = 96dpi</param>
        /// <returns></returns>
        public async ValueTask<Data.PdfPage[]?> PdfToImagesAsync(string pdfAsBase64, double scale = 1.5d)
        {
            try
            {
                if (_pdfHelperJs is null)
                {
                    _pdfHelperJs = await _js.InvokeAsync<IJSObjectReference>("import", "./_content/FreeRP.Net.Client/pdfHelper/pdfHelper.js");
                }

                return await _pdfHelperJs.InvokeAsync<Data.PdfPage[]>("getPdfPages", pdfAsBase64, "", scale);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
