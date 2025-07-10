using FreeRP.Net.Client.Translation;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace FreeRP.Net.Client.Blazor.Pages.Admin
{
    public partial class Users : IDisposable
    {
        [Inject] private GrpcServices.AdminService AdminService { get; set; } = default!;
        [Inject] private IDialogService Dlg { get; set; } = default!;
        [Inject] private Dialog.BusyDialogService BusyDialogService { get; set; } = default!;
        [Inject] private I18nService I18n { get; set; } = default!;

        readonly DialogParameters para = [];

        IQueryable<GrpcService.Core.User>? _users;
        readonly GridSort<GrpcService.Core.User> lockSort = GridSort<GrpcService.Core.User>.ByDescending(x => x.IsLock);
        readonly GridSort<GrpcService.Core.User> devSort = GridSort<GrpcService.Core.User>.ByDescending(x => x.IsDeveloper);
        readonly GridSort<GrpcService.Core.User> apiSort = GridSort<GrpcService.Core.User>.ByDescending(x => x.IsApi);

        protected override void OnParametersSet()
        {
            if (_users is null)
            {
                _users = AdminService.AdminData.Users.AsQueryable();
            }
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                I18n.TextChanged += UpdateUI;
                AdminService.AdminDataChanged += AdminDataChanged;
            }
        }

        private void AdminDataChanged(object? sender, GrpcService.Admin.AdminData e)
        {
            _users = AdminService.AdminData.Users.AsQueryable();
            UpdateUI();
        }

        private void UpdateUI()
        {
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            I18n.TextChanged -= UpdateUI;
            AdminService.AdminDataChanged -= AdminDataChanged;
            GC.SuppressFinalize(this);
        }

        private async Task UserAddClick()
        {
            var _dlgRef = await Dlg.ShowDialogAsync<Dialog.EditUserDialog>(new GrpcService.Core.User() { Theme = new() { BaseLayerLuminance = 0, AccentBaseColor = "#1E90FF" } }, para);
            await _dlgRef.Result;
        }

        private async Task UserApiAddClick()
        {
            var _dlgRef = await Dlg.ShowDialogAsync<Dialog.EditUserApiDialog>(new GrpcService.Core.User() { IsApi = true, Theme = new() { BaseLayerLuminance = 0, AccentBaseColor = "#1E90FF" } }, para);
            await _dlgRef.Result;
        }

        private async Task UserEdit(GrpcService.Core.User user)
        {
            if (user.IsApi)
            {
                var _dlgRef = await Dlg.ShowDialogAsync<Dialog.EditUserApiDialog>(user, para);
                await _dlgRef.Result;
            }
            else
            {
                var _dlgRef = await Dlg.ShowDialogAsync<Dialog.EditUserDialog>(user, para);
                await _dlgRef.Result;
            }
        }
    }
}
