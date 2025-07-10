using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Client.Dialog
{
    public interface IBusyDialogService
    {
        Task ShowBusyAsync(string title = "", string loadingText = "");
        Task UpdateTextAsync(string loadingText = "");
        Task CloseBusyAsync();
    }
}
