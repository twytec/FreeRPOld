using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace FreeRP.Net.Client.Blazor.Dialog
{
    public class BusyDialogService(IDialogService dlg)
    {
        private readonly IDialogService _dlg = dlg;
        private IDialogReference? _dialog;
        private readonly DialogParameters<SplashScreenContent> _para = new()
        {
            Content = new()
            {
                Message = (MarkupString)"<FluentProgressRing></FluentProgressRing>"
            }
        };

        public async Task ShowBusyAsync(string title = "", string loadingText = "")
        {
            _para.Content.Title = title;
            _para.Content.LoadingText = loadingText;
            _dialog = await _dlg.ShowSplashScreenAsync(_para);
        }

        public async Task UpdateTextAsync(string loadingText = "")
        {
            if (_dialog is not null)
            {
                _para.Content.LoadingText = loadingText;
                await _dlg.UpdateDialogAsync(_dialog.Id, _para);
            }
        }

        public async Task CloseBusyAsync()
        {
            if (_dialog is not null)
            {
                await _dialog.CloseAsync();
            }
        }
    }
}
