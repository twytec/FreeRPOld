using FreeRP.Content;
using MudBlazor;
using System.Xml.Linq;
using System;

namespace FreeRP.Server.Components.Pages.Admin.Contents
{
    public partial class Contents
    {
        private string _selectedUri = "file://";

        private MudDataGrid<FrpContentItem>? _grid;
        private FrpContentItems? _tree;

        FrpContentItem? _copyCutContentItem;
        private bool _copy = false;

        private bool _hideDetail = false;
        private void ShowDetail() => _hideDetail = false;
        private void ShowNameOnly() => _hideDetail = true;

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                auth.OnLogin += (s, e) => LoginChange();
                LoginChange();
            }
        }

        private async void LoginChange()
        {
            if (auth.IsAdmin)
            {
                await LoadContentAsync();
                StateHasChanged();
            }
        }

        private async Task LoadContentAsync()
        {
            var res = await ds.FrpContentService.GetContentItemsAsync(new() { Uri = _selectedUri }, auth.FrpAuthService);
            _tree = res.AnyData.Unpack<FrpContentItems>();
        }

        private async Task OpenContentItem(FrpContentItem item)
        {
            if (item.IsFile)
            {
                
            }
            else
            {
                _selectedUri = item.Uri;
                await LoadContentAsync();
            }
        }

        #region Cut, copy, paste

        private void Copy(FrpContentItem item)
        {
            _copyCutContentItem = item;
            _copy = true;
        }

        private void Cut(FrpContentItem item)
        {
            _copyCutContentItem = item;
            _copy = false;
        }

        private async Task Paste()
        {
            if (_tree is not null && _copyCutContentItem is not null)
            {
                FrpMoveContentUriRequest req = new()
                {
                    Copy = _copy,
                    DestUri = _selectedUri + _copyCutContentItem.Name,
                    SourceUri = _copyCutContentItem.Uri,
                };

                FrpResponse res;
                if (_copyCutContentItem.IsFile)
                    res = await ds.FrpContentService.MoveFileAsync(req, auth.FrpAuthService);
                else
                    res = await ds.FrpContentService.MoveDirectoryAsync(req, auth.FrpAuthService);

                if (res.ErrorType == FrpErrorType.ErrorDirectoryExist || res.ErrorType == FrpErrorType.ErrorFileExist)
                {
                    var yes = await dlg.ShowMessageBox(_copyCutContentItem.Name, $"{res.Message} {i18n.Text.Replace}", i18n.Text.Yes, i18n.Text.No);
                    if (yes == true)
                    {
                        req.Replace = true;
                        if (_copyCutContentItem.IsFile)
                            res = await ds.FrpContentService.MoveFileAsync(req, auth.FrpAuthService);
                        else
                            res = await ds.FrpContentService.MoveDirectoryAsync(req, auth.FrpAuthService);

                        if (res.ErrorType == FrpErrorType.ErrorNone)
                        {
                            await LoadContentAsync();
                        }
                        else
                        {
                            await dlg.ShowMessageBox(i18n.Text.Error, res.Message, i18n.Text.Ok);
                        }
                    }
                }
                else if (res.ErrorType is not FrpErrorType.ErrorNone)
                {
                    await dlg.ShowMessageBox(i18n.Text.Error, res.Message, i18n.Text.Ok);
                }
                else
                {
                    if (res.ErrorType == FrpErrorType.ErrorNone)
                    {
                        await LoadContentAsync();
                    }
                    else
                    {
                        await dlg.ShowMessageBox(i18n.Text.Error, res.Message, i18n.Text.Ok);
                    }
                }
            }
            _copyCutContentItem = null;
        }

        private async Task Duplicate(FrpContentItem item)
        {
            var para = new DialogParameters<Pages.Dialogs.EditTextDialog>()
            {
                { x => x.Label, i18n.Text.Name },
                { x => x.Text, item.Name }
            };

            var dr = await dlg.ShowAsync<Pages.Dialogs.EditTextDialog>(i18n.Text.DirectoryCreate, para);
            var dlgRes = await dr.Result;
            if (dlgRes is not null && dlgRes.Canceled == false && dlgRes.Data is string s && string.IsNullOrEmpty(s) == false)
            {
                FrpMoveContentUriRequest req = new()
                {
                    Duplicate = true,
                    DestUri = _selectedUri + s,
                    SourceUri = item.Uri,
                };

                FrpResponse res;
                if (item.IsFile)
                    res = await ds.FrpContentService.MoveFileAsync(req, auth.FrpAuthService);
                else
                    res = await ds.FrpContentService.MoveDirectoryAsync(req, auth.FrpAuthService);

                if (res.ErrorType == FrpErrorType.ErrorNone)
                    await LoadContentAsync();
                else
                    await dlg.ShowMessageBox(i18n.Text.Error, res.Message, i18n.Text.Ok);
            }
        }

        #endregion

        #region Add, create

        private async Task AddFile()
        {
            var para = new DialogParameters<Dialogs.UploadFileDialog>() {
                { x => x.Uri, _selectedUri }
            };

            var dr = await dlg.ShowAsync<Dialogs.UploadFileDialog>(i18n.Text.FileUpload, para);
            await dr.Result;
            await LoadContentAsync();
        }

        private async Task CreateDirectory()
        {
            var para = new DialogParameters<Pages.Dialogs.EditTextDialog>()
            {
                { x => x.Label, i18n.Text.Name }
            };

            var dr = await dlg.ShowAsync<Pages.Dialogs.EditTextDialog>(i18n.Text.DirectoryCreate, para);
            var dlgRes = await dr.Result;
            if (dlgRes is not null && dlgRes.Canceled == false && dlgRes.Data is string s && string.IsNullOrEmpty(s) == false)
            {
                var u = $"{_selectedUri}{s}";
                var res = await ds.FrpContentService.CreateDirectoryAsync(new() { Uri = u }, auth.FrpAuthService);
                if (res.ErrorType == FrpErrorType.ErrorNone)
                {
                    await LoadContentAsync();
                }
                else
                {
                    await dlg.ShowMessageBox(i18n.Text.Error, res.Message, i18n.Text.Ok);
                }
            }
        }

        #endregion

        #region Change, delete, download

        private async Task Change(FrpContentItem item)
        {
            var para = new DialogParameters<Pages.Dialogs.EditTextDialog>()
                {
                    { x => x.Label, i18n.Text.Name },
                    { x => x.Text, item.Name }
                };

            var dr = await dlg.ShowAsync<Pages.Dialogs.EditTextDialog>(i18n.Text.DirectoryChange, para);
            var dlgRes = await dr.Result;
            if (dlgRes is not null && dlgRes.Canceled == false && dlgRes.Data is string s && string.IsNullOrEmpty(s) == false)
            {
                FrpMoveContentUriRequest u = new()
                {
                    SourceUri = item.Uri,
                    DestUri = _selectedUri + s,
                };
                FrpResponse res;
                if (item.IsFile)
                    res = await ds.FrpContentService.MoveFileAsync(u, auth.FrpAuthService);
                else
                    res = await ds.FrpContentService.MoveDirectoryAsync(u, auth.FrpAuthService);

                if (res.ErrorType == FrpErrorType.ErrorNone)
                {
                    await LoadContentAsync();
                }
                else
                {
                    await dlg.ShowMessageBox(i18n.Text.Error, res.Message, i18n.Text.Ok);
                }
            }
        }

        private async Task Delete(FrpContentItem item)
        {
            if (await dlg.ShowMessageBox(item.Name, i18n.Text.ReallyDelete, i18n.Text.Yes, i18n.Text.No) == true)
            {
                FrpContentUriRequest u = new()
                {
                    Uri = item.Uri,
                };
                FrpResponse res;
                if (item.IsFile)
                    res = await ds.FrpContentService.DeleteFileAsync(u, auth.FrpAuthService);
                else
                    res = await ds.FrpContentService.DeleteDirectoryAsync(u, auth.FrpAuthService);

                if (res.ErrorType == FrpErrorType.ErrorNone)
                {
                    await LoadContentAsync();
                }
                else
                {
                    await dlg.ShowMessageBox(i18n.Text.Error, res.Message, i18n.Text.Ok);
                }
            }
        }

        private async Task Download(FrpContentItem item)
        {
            var res = await ds.FrpContentService.DownloadAsync(new() { Uri = item.Uri }, auth.FrpAuthService);
            if (res.ErrorType is FrpErrorType.ErrorNone)
            {
                string fileName = item.Name;
                if (item.IsFile == false)
                    fileName += ".zip";

                await js.InvokeVoidAsyncIgnoreErrors("triggerFileDownload", fileName, res.Data);
            }
            else
            {
                await dlg.ShowMessageBox(i18n.Text.Error, res.Message, i18n.Text.Ok);
            }
        }

        #endregion

        private async Task ContentPermission(string uri)
        {
            var para = new DialogParameters<Dialogs.PermissionDialog>() {
                    { x => x.Uri, uri },
                };

            var dr = await dlg.ShowAsync<Dialogs.PermissionDialog>("", para, new() { CloseButton = true });
            await dr.Result;
        }
    }
}
