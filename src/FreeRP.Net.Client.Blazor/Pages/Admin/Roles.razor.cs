using FreeRP.Net.Client.Translation;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace FreeRP.Net.Client.Blazor.Pages.Admin
{
    public partial class Roles : IDisposable
    {
        [Inject] private GrpcServices.AdminService AdminService { get; set; } = default!;
        [Inject] private IDialogService Dlg { get; set; } = default!;
        [Inject] private Dialog.BusyDialogService BusyDialogService { get; set; } = default!;
        [Inject] private I18nService I18n { get; set; } = default!;

        GrpcService.Core.Role? _role;
        readonly Dictionary<string, string> _users = [];

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                I18n.TextChanged += UpdateUI;
                AdminService.AdminDataChanged += AdminDataChanged;
            }
        }

        private void AdminDataChanged(object? sender, GrpcService.Admin.AdminData e) => UpdateUI();

        private void UpdateUI()
        {
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            AdminService.AdminDataChanged -= AdminDataChanged;
            I18n.TextChanged -= UpdateUI;
            GC.SuppressFinalize(this);
        }

        private void RoleChange(GrpcService.Core.Role role)
        {
            _role = role;
            _users.Clear();
            var uirs = AdminService.AdminData.UserInRoles.Where(x => x.RoleId == _role.RoleId).ToArray();
            if (uirs.Length > 0)
            {
                foreach (var item in uirs)
                {
                    if (AdminService.AdminData.Users.FirstOrDefault(x => x.UserId == item.UserId) is GrpcService.Core.User u)
                    {
                        _users[u.UserId] = $"{u.Email} {u.FirstName} {u.LastName}";
                    }
                }
            }
        }

        private async Task RoleAddClick()
        {
            Data.TextEdit textEdit = new() { Label = I18n.Text.Name, FieldType = TextFieldType.Text };
            DialogParameters para = new() { Title = I18n.Text.AddRole, PrimaryAction = I18n.Text.Ok };
            var dlgRef = await Dlg.ShowDialogAsync<Dialog.TextEditDialog>(textEdit, para);
            var dlgRes = await dlgRef.Result;

            if (dlgRes.Cancelled == false && textEdit.Text is not null)
            {
                await BusyDialogService.ShowBusyAsync();
                var res = await AdminService.RoleAddAsync(new GrpcService.Core.Role() { Name = textEdit.Text.Trim() });
                await BusyDialogService.CloseBusyAsync();

                if (res is not null)
                {
                    dlgRef = await Dlg.ShowErrorAsync(res);
                    await dlgRef.Result;
                }
            }
        }

        private async Task RoleEditClick()
        {
            if (_role is null)
            {
                return;
            }

            Data.TextEdit textEdit = new() { Text = _role.Name, Label = I18n.Text.Name, FieldType = TextFieldType.Text };
            DialogParameters para = new() { Title = I18n.Text.ChangeRole, PrimaryAction = I18n.Text.Ok };
            var dlgRef = await Dlg.ShowDialogAsync<Dialog.TextEditDialog>(textEdit, para);
            var dlgRes = await dlgRef.Result;

            if (dlgRes.Cancelled == false && textEdit.Text is not null && textEdit.Text != _role.Name)
            {
                await BusyDialogService.ShowBusyAsync();
                _role.Name = textEdit.Text;
                var res = await AdminService.RoleChangeAsync(_role);
                await BusyDialogService.CloseBusyAsync();

                if (res is not null)
                {
                    dlgRef = await Dlg.ShowErrorAsync(res);
                    await dlgRef.Result;
                }
            }
        }

        private async Task RoleDeleteClick()
        {
            if (_role is null)
            {
                return;
            }

            var dlgRef = await Dlg.ShowConfirmationAsync(I18n.Text.ReallyDelete, I18n.Text.Yes, I18n.Text.No, _role.Name);
            var dlgRes = await dlgRef.Result;

            if (dlgRes.Cancelled == false)
            {
                await BusyDialogService.ShowBusyAsync();
                var res = await AdminService.RoleDeleteAsync(_role);
                await BusyDialogService.CloseBusyAsync();

                if (res is not null)
                {
                    dlgRef = await Dlg.ShowErrorAsync(res);
                    await dlgRef.Result;
                }
            }
        }

        private async Task AddUser()
        {
            if (_role is null)
            {
                return;
            }

            List<Data.PickItem> dict = [];
            foreach (var u in AdminService.AdminData.Users)
            {
                if (_users.ContainsKey(u.UserId) == false)
                {
                    dict.Add(new(u.UserId, u.Email, u));
                }
            }

            DialogParameters para = new() { Title = I18n.Text.User };
            var dlgRef = await Dlg.ShowDialogAsync<Dialog.ItemPickDialog>(dict, para);
            var dlgRes = await dlgRef.GetReturnValueAsync<Data.PickItem>();

            if (dlgRes is not null)
            {
                await BusyDialogService.ShowBusyAsync();
                var res = await AdminService.RoleAddUserAsync(new() { RoleId = _role.RoleId, UserId = dlgRes.Key });
                await BusyDialogService.CloseBusyAsync();

                if (res is not null)
                {
                    dlgRef = await Dlg.ShowErrorAsync(res);
                    await dlgRef.Result;
                }
                else if (dlgRes.Data is GrpcService.Core.User u)
                {
                    _users[u.UserId] = $"{u.Email} {u.FirstName} {u.LastName}";
                }
                else
                {
                    RoleChange(_role);
                }
            }
        }

        private async Task DeleteUser(string key)
        {
            if (_role is null)
            {
                return;
            }

            if (AdminService.AdminData.UserInRoles.FirstOrDefault(x => x.UserId == key && x.RoleId == _role.RoleId) is GrpcService.Core.UserInRole uir)
            {
                var dlgRef = await Dlg.ShowConfirmationAsync(I18n.Text.ReallyDelete, I18n.Text.Yes, I18n.Text.No, I18n.Text.UserInRole);
                var dlgRes = await dlgRef.Result;

                if (dlgRes.Cancelled == false)
                {
                    await BusyDialogService.ShowBusyAsync();
                    var res = await AdminService.RoleDeleteUserAsync(uir);
                    await BusyDialogService.CloseBusyAsync();

                    if (res is not null)
                    {
                        dlgRef = await Dlg.ShowErrorAsync(res);
                        await dlgRef.Result;
                    }
                    else
                    {
                        _users.Remove(key);
                    }
                }
            }
        }
    }
}
