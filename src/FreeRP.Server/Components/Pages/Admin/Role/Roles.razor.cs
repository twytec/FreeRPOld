using FreeRP.Role;
using FreeRP.User;
using MudBlazor;

namespace FreeRP.Server.Components.Pages.Admin.Role
{
    public partial class Roles
    {
        private readonly List<FrpRole> _roles = [];
        private readonly List<FrpUser> _userInRole = [];
        private readonly List<FrpUser> _userNotInRole = [];

        private FrpRole? _selectedRole;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                auth.OnLogin += async (s, e) => await LoginChange();
                await LoginChange();
            }
        }

        private async Task LoginChange()
        {
            if (auth.IsAdmin)
            {
                await LoadRolesAsync();
                StateHasChanged();
            }
        }

        private async Task LoadRolesAsync()
        {
            _roles.Clear();
            _roles.AddRange((await ds.FrpRoleService.GetAllRolesAsync()).OrderBy(x => x.Name));
        }

        private async Task RoleSelectChangeAsync(string id)
        {
            _selectedRole = _roles.First(x => x.RoleId == id);
            _userInRole.Clear();
            _userNotInRole.Clear();

            foreach (var u in await ds.FrpUserService.GetAllUsersAsync())
            {
                if (await ds.FrpRoleService.IsUserInRoleAsync(u.UserId, _selectedRole.RoleId))
                {
                    _userInRole.Add(u);
                }
                else
                {
                    _userNotInRole.Add(u);
                }
            }
        }

        private async Task AddRole()
        {
            var para = new DialogParameters<Dialogs.EditTextDialog>
            {
                { x => x.Label, i18n.Text.Name }
            };

            while (true)
            {
                var dialog = await dlg.ShowAsync<Dialogs.EditTextDialog>(i18n.Text.Role, para);
                var res = await dialog.Result;

                if (res is null || res.Canceled)
                {
                    break;
                }
                else if (res.Data is string s)
                {
                    var r = await ds.FrpRoleService.AddRoleAsync(new FrpRole() { Name = s }, auth.FrpAuthService);
                    if (r.ErrorType == FrpErrorType.ErrorNone)
                    {
                        await LoadRolesAsync();
                        break;
                    }
                    else
                    {
                        await dlg.ShowMessageBox(i18n.Text.Error, r.Message, i18n.Text.Ok);
                        para.Add(x => x.Text, s);
                    }
                }
                else
                {
                    await dlg.ShowMessageBox(i18n.Text.Error, i18n.Text.ErrorRoleNameRequired, i18n.Text.Ok);
                }
            }
            StateHasChanged();
        }

        private async Task EditRole()
        {
            if (_selectedRole is null)
                return;

            var para = new DialogParameters<Dialogs.EditTextDialog>
            {
                { x => x.Label, i18n.Text.Name },
                { x => x.Text, _selectedRole.Name }
            };

            while (true)
            {
                var dialog = await dlg.ShowAsync<Dialogs.EditTextDialog>(i18n.Text.Role, para);
                var res = await dialog.Result;

                if (res is null || res.Canceled)
                {
                    break;
                }
                else if (res.Data is string s)
                {
                    _selectedRole.Name = s;
                    var r = await ds.FrpRoleService.ChangeRoleAsync(_selectedRole, auth.FrpAuthService);
                    if (r.ErrorType == FrpErrorType.ErrorNone)
                    {
                        await LoadRolesAsync();
                        break;
                    }
                    else
                    {
                        await dlg.ShowMessageBox(i18n.Text.Error, r.Message, i18n.Text.Ok);
                        para.Add(x => x.Text, s);
                    }
                }
                else
                {
                    await dlg.ShowMessageBox(i18n.Text.Error, i18n.Text.ErrorRoleNameRequired, i18n.Text.Ok);
                }
            }
        }

        private async Task DeleteRole()
        {
            if (_selectedRole is null)
                return;

            var yes = await dlg.ShowMessageBox(_selectedRole.Name, i18n.Text.ReallyDelete, i18n.Text.Yes, i18n.Text.No);
            if (yes == true)
            {
                var res = await ds.FrpRoleService.DeleteRoleAsync(_selectedRole, auth.FrpAuthService);
                if (res.ErrorType == FrpErrorType.ErrorNone)
                {
                    await LoadRolesAsync();
                    _selectedRole = null;
                }
                else
                {
                    await dlg.ShowMessageBox(i18n.Text.Error, res.Message, i18n.Text.Ok);
                }
            }
        }

        private async Task AddUserToRole()
        {
            if (_selectedRole is null || _userNotInRole.Count == 0)
                return;

            Dictionary<string, string> dict = [];
            foreach (var u in _userNotInRole)
            {
                dict.Add(u.UserId, u.Email);
            }

            var para = new DialogParameters<Dialogs.ItemPickerDialog>()
            {
                { x => x.Content, dict }
            };

            var dialog = await dlg.ShowAsync<Dialogs.ItemPickerDialog>(i18n.Text.UserAdd, para);
            var res = await dialog.Result;

            if (res is not null && res.Canceled == false && res.Data is string s)
            {
                var er = await ds.FrpRoleService.AddUserToRoleAsync(new() { UserId = s, RoleId = _selectedRole.RoleId }, auth.FrpAuthService);
                if (er.ErrorType == FrpErrorType.ErrorNone)
                {
                    await RoleSelectChangeAsync(_selectedRole.RoleId);
                }
                else
                {
                    await dlg.ShowMessageBox(i18n.Text.Error, er.Message, i18n.Text.Ok);
                }
            }
        }

        private async Task DeleteUserFromRole(FrpUser u)
        {
            if (_selectedRole is null)
                return;

            var ok = await dlg.ShowMessageBox(i18n.Text.ReallyDelete, u.Email, i18n.Text.Yes, i18n.Text.No);
            if (ok == true)
            {
                var res = await ds.FrpRoleService.DeleteUserFromRoleAsync(
                    new() { RoleId = _selectedRole.RoleId, UserId = u.UserId }, auth.FrpAuthService);

                if (res.ErrorType == FrpErrorType.ErrorNone)
                {
                    await RoleSelectChangeAsync(_selectedRole.RoleId);
                }
                else
                {
                    await dlg.ShowMessageBox(i18n.Text.Error, res.Message, i18n.Text.Ok);
                }
            }
        }
    }
}
