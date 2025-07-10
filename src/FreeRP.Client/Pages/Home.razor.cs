
using FreeRP.Plugins;

namespace FreeRP.Client.Pages
{
    public partial class Home : IDisposable
    {
        private const string TokenKey = "FrpTokenKy";
        private bool _isDarkMode = true;
        private bool _drawerOpen = false;
        private bool _workConnectProcess = false;
        private FrpPlugin? _frpPlugin;

        protected override void OnParametersSet()
        {
            _ds.FrpAuthService.Connected += FrpAuthService_Connected;
            _ds.FrpAuthService.Disconnected += FrpAuthService_Disconnected;
            _ds.FrpAuthService.I18n.TextChanged += I18n_TextChanged;
        }

        private void I18n_TextChanged()
        {
            InvokeAsync(() => StateHasChanged());
        }

        public void Dispose()
        {
            _ds.FrpAuthService.Connected -= FrpAuthService_Connected;
            _ds.FrpAuthService.Disconnected -= FrpAuthService_Disconnected;
            _ds.FrpAuthService.I18n.TextChanged -= I18n_TextChanged;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (_settings.FrpServers.Count == 0)
                {
                    while (true)
                    {
                        var dialog = await _dlg.ShowAsync<Dialogs.ConnectToServerDialog>(_ds.FrpAuthService.I18n.Text.ServerUrl);
                        var res = await dialog.Result;
                        if (res is not null && res.Data is string s)
                        {
                            try
                            {
                                var resp = await _ds.FrpAuthService.ConnectAsync(s);
                                if (resp is not null)
                                    break;
                            }
                            catch (Exception ex)
                            {
                                await _dlg.ShowMessageBox(_ds.FrpAuthService.I18n.Text.Error, ex.Message, _ds.FrpAuthService.I18n.Text.Ok);
                            }
                        }
                    }
                }
                else
                {
                    try
                    {
                        await _ds.FrpAuthService.ConnectAsync(string.Empty);
                    }
                    catch (Exception ex)
                    {
                        await _dlg.ShowMessageBox(_ds.FrpAuthService.I18n.Text.Error, ex.Message, _ds.FrpAuthService.I18n.Text.Ok);
                    }
                }
            }
        }

        private async void FrpAuthService_Connected(object? sender, FrpServices.IFrpAuthService e)
        {
            if (_workConnectProcess == false)
            {
                _workConnectProcess = true;

                bool isLogin = false;
                if (e.IsLogin)
                {
                    try
                    {
                        await e.PingServerAsync(new() { AnyData = "" });
                        isLogin = true;
                    }
                    catch (Exception)
                    {
                    }
                }

                if (isLogin == false)
                {
                    try
                    {
                        var token = await ls.GetPreferencesAsync(TokenKey);
                        if (token is not null)
                        {
                            await _ds.FrpAuthService.LoginWithTokenAsync(token);
                            isLogin = true;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                if (isLogin == false)
                {
                    MudBlazor.DialogOptions opt = new() { CloseButton = false };
                    var dialog = await _dlg.ShowAsync<Dialogs.LoginDialog>(_ds.FrpAuthService.I18n.Text.Login, opt);
                    await dialog.Result;
                    isLogin = true;
                }

                if (isLogin)
                {
                    _isDarkMode = _ds.FrpAuthService.User.Theme.DarkMode;
                    StateHasChanged();
                }

                _workConnectProcess = false;
            }
        }

        private void FrpAuthService_Disconnected(object? sender, FrpServices.IFrpAuthService e)
        {
        }

        

        private void WebTopClick()
        {
        }

        private void LoadPlugin(FrpPlugin plugin)
        {

        }
    }
}
