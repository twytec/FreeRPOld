using FreeRP.Net.Client.Blazor.Data;
using FreeRP.Net.Client.Translation;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components.DesignTokens;

namespace FreeRP.Net.Client.Blazor.Pages
{
    public partial class Home
    {
        [Inject] private GrpcServices.ConnectService HelloService { get; set; } = default!;

        [Inject] private GrpcServices.AdminService AdminService { get; set; } = default!;

        [Inject] private GrpcServices.UserService UserService { get; set; } = default!;

        [Inject] private IDialogService Dlg { get; set; } = default!;

        [Inject] private Dialog.BusyDialogService BusyDialogService { get; set; } = default!;

        [Inject] private FrpEnvironment Env { get; set; } = default!;

        [Inject] private I18nService I18n { get; set; } = default!;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                I18n.TextChanged += UpdateUI;
                await ConnectToServerAsync();
            }
        }

        private void UpdateUI()
        {
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            I18n.TextChanged -= UpdateUI;
        }

        private async Task ConnectToServerAsync()
        {
            await BusyDialogService.ShowBusyAsync(I18n.Text.Loading, I18n.Text.ConnectToServer);

            var ac = await Env.GetAppConfigAsync();
            bool connected = false;
            if (ac.ServerUrls.Length != 0)
            {
                foreach (var item in ac.ServerUrls)
                {
                    connected = await HelloService.TryConnectAsync(item);
                    if (connected)
                    {
                        break;
                    }
                }
            }

            await BusyDialogService.CloseBusyAsync();

            if (connected == false)
            {
                while (true)
                {
                    Data.TextEdit textEdit = new() { Label = I18n.Text.Url, Required = true, FieldType = TextFieldType.Url };
                    DialogParameters para = new() { Title = I18n.Text.ServerUrl, PrimaryAction = I18n.Text.Ok };
                    var dlgRef = await Dlg.ShowDialogAsync<Dialog.TextEditDialog>(textEdit, para);
                    await dlgRef.Result;

                    if (textEdit.Text is not null && await HelloService.TryConnectAsync(textEdit.Text))
                    {
                        break;
                    }
                    else
                    {
                        var errRef = await Dlg.ShowErrorAsync(I18n.Text.ConnectToServerError, I18n.Text.Error);
                        await errRef.Result;
                    }
                }
            }
            else
            {
                DialogParameters para = [];
                var dlgRef = await Dlg.ShowDialogAsync<Dialog.LoginDialog>(para);
                await dlgRef.Result;
                StateHasChanged();
            }
        }
    }
}
