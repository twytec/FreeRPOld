using FreeRP.Database;
using FreeRP.Server.Components.Pages.Admin.Databases.Dialogs;
using MudBlazor;

namespace FreeRP.Server.Components.Pages.Admin.Databases
{
    public partial class Databases
    {
        private readonly List<FrpDatabase> _databases = [];

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                auth.OnLogin += async (s, e) => await LoginChangeAsync();
                await LoginChangeAsync();
            }
        }

        private async Task LoginChangeAsync()
        {
            if (auth.IsAdmin)
            {
                await LoadDatabasesAsync();
                StateHasChanged();
            }
        }

        private async Task LoadDatabasesAsync()
        {
            _databases.Clear();
            var dbs = await ds.FrpDatabaseService.GetAllDatabasesAsync();
            _databases.AddRange(dbs.OrderBy(x => x.Name));
        }

        private async Task AddDatabase()
        {
            var para = new DialogParameters<Pages.Dialogs.EditTextDialog>
            {
                { x => x.Label, i18n.Text.Id }
            };

            while (true)
            {
                var dialog = await dlg.ShowAsync<Pages.Dialogs.EditTextDialog>(i18n.Text.Database, para);
                var res = await dialog.Result;

                if (res is null || res.Canceled)
                {
                    break;
                }
                else if (res is not null && res.Data is string s)
                {
                    FrpDatabase db = new() { DatabaseId = s };

                    var r = await ds.FrpDatabaseService.AddDatabaseAsync(db, auth.FrpAuthService);
                    if (r.ErrorType == FrpErrorType.ErrorNone)
                    {
                        await LoadDatabasesAsync();
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

        private async Task ChangeDatabase(FrpDatabase db)
        {
            var clone = db.Clone();
            var para = new DialogParameters<ChangeDatabaseDialog>
            {
                { x => x.Content, clone }
            };

            DialogOptions opt = new() { 
                FullScreen = true,
                CloseButton = false,
                NoHeader = true,
            };

            while (true)
            {
                var dialog = await dlg.ShowAsync<ChangeDatabaseDialog>(i18n.Text.Database, para, opt);
                var res = await dialog.Result;

                if (res is null || res.Canceled)
                {
                    break;
                }
                else
                {
                    var r = await ds.FrpDatabaseService.ChangeDatabaseAsync(clone, auth.FrpAuthService);
                    if (r.ErrorType == FrpErrorType.ErrorNone)
                    {
                        await LoadDatabasesAsync();
                        break;
                    }
                    else
                    {
                        await dlg.ShowMessageBox(i18n.Text.Error, r.Message, i18n.Text.Ok);
                    }
                }
            }
        }

        private async Task DeleteDatabase(FrpDatabase db)
        {
            var q = await dlg.ShowMessageBox(db.GetName(), i18n.Text.ReallyDelete, i18n.Text.Yes, i18n.Text.No);
            if (q == true)
            {
                var res = await ds.FrpDatabaseService.DeleteDatabaseAsync(db, auth.FrpAuthService);
                if (res.ErrorType == FrpErrorType.ErrorNone)
                {
                    await LoadDatabasesAsync();
                }
                else
                {
                    await dlg.ShowMessageBox(i18n.Text.Error, res.Message, i18n.Text.Ok);
                }
            }
        }

        private async Task ChangeAccess(FrpDatabase db)
        {
            var clone = db.Clone();
            var para = new DialogParameters<DatabaseAccessDialog>
            {
                { x => x.Content, clone }
            };

            DialogOptions opt = new()
            {
                FullScreen = true,
                CloseButton = false,
                NoHeader = true,
            };

            var dialog = await dlg.ShowAsync<DatabaseAccessDialog>(i18n.Text.Database, para, opt);
            var res = await dialog.Result;
        }
    }
}
