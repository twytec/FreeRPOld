using FreeRP.Database;
using FreeRP.Role;
using FreeRP.User;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FreeRP.Server.Components.Pages.Admin.Databases.Dialogs
{
    public partial class DatabaseAccessDialog
    {
        [CascadingParameter]
        public MudDialogInstance Dialog { get; set; } = default!;

        [Parameter]
        public FrpDatabase Content { get; set; } = default!;

        private bool _init = false;
        private IEnumerable<FrpUser>? _users;
        private IEnumerable<FrpRole>? _roles;
        private IEnumerable<FrpPermission>? _accesses;
        private FrpDatabasePermissions? _viewModel;

        protected override async Task OnParametersSetAsync()
        {
            if (_init) return;
            _init = true;

            if (Content.AccessMode == FrpAccessMode.AccessModeRole)
                _roles = await ds.FrpRoleService.GetAllRolesAsync();
            else if (Content.AccessMode == FrpAccessMode.AccessModeUser)
                _users = await ds.FrpUserService.GetAllUsersAsync();

            _accesses = await ds.FrpPermissionService.GetDatabasePermissionsAsync(Content.DatabaseId);
            _viewModel = new(Content);
        }

        void Cancel() => Dialog.Cancel();

        private void UserSelectChanged(string id)
        {
            if (_viewModel is not null && _accesses is not null)
                _viewModel.SetPermission(id, MemberIdKind.User, _accesses);
        }

        private void RoleSelectChanged(string id)
        {
            if (_viewModel is not null && _accesses is not null)
                _viewModel.SetPermission(id, MemberIdKind.Role, _accesses);
        }

        async Task Save()
        {
            //busy.IsVisable = true;

            if (_viewModel is not null && _viewModel.Loaded)
            {
                var res = await _viewModel.SaveAsync(ds.FrpPermissionService, auth.FrpAuthService);
                if (res is not null && res.ErrorType is not FrpErrorType.ErrorNone)
                    await dlg.ShowMessageBox(i18n.Text.Error, res.Message, i18n.Text.Ok);
            }

            //busy.IsVisable = false;
            Dialog.Close();
        }
    }
}
