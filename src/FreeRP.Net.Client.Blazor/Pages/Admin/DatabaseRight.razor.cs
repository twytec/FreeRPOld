using FreeRP.Net.Client.Blazor.Dialog;
using FreeRP.Net.Client.Translation;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using System.ComponentModel.Design;
using System.Threading.Tasks.Sources;

namespace FreeRP.Net.Client.Blazor.Pages.Admin
{
    public sealed partial class DatabaseRight
    {
        [Inject] private GrpcServices.AdminService AdminService { get; set; } = default!;
        [Inject] private GrpcServices.DatabaseService DatabaseService { get; set; } = default!;
        [Inject] private IDialogService Dlg { get; set; } = default!;
        [Inject] private Dialog.BusyDialogService BusyDialogService { get; set; } = default!;
        [Inject] private I18nService I18n { get; set; } = default!;

        private readonly DialogParameters para = [];

        private string _selectRoleValue = "";
        private string _selectDatabaseValue = "";
        private string _selectTableValue = "";

        private GrpcService.Core.Role? _role;
        private GrpcService.Database.Database? _database;
        private GrpcService.Database.DatabaseTable? _databaseTable;
        private readonly List<GrpcService.Core.Right> _rights = [];

        private IQueryable<GrpcService.Database.DatabaseTableField>? _fields;
        readonly GridSort<GrpcService.Database.DatabaseTableField> _dataTypeSort =
            GridSort<GrpcService.Database.DatabaseTableField>.ByDescending(x => x.DataType);

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
            InvokeAsync(() =>
            {
                StateHasChanged();
            });
        }

        public void Dispose()
        {
            I18n.TextChanged -= UpdateUI;
            AdminService.AdminDataChanged -= AdminDataChanged;
        }

        private void SelectRole(GrpcService.Core.Role? role)
        {
            _role = role;
            _rights.Clear();

            if (_role is null)
            {
                _selectRoleValue = "";
                SelectDatabase(null);
            }
        }

        private void SelectDatabase(GrpcService.Database.Database? db)
        {
            if (_role is null || db is null)
            {
                _selectDatabaseValue = "";
                _database = null;
                SelectTable(null);
                return;
            }

            _database = db.Clone();

            var r = _role.Rights.Where(x => x.ForeignKey.StartsWith(_database.DatabaseId));
            if (r.Any())
            {
                _rights.AddRange(r);

                var dr = _rights.FirstOrDefault(x => x.ForeignKey == _database.DatabaseId);
                if (dr is not null)
                {
                    _database.Change = dr.Change;
                    _database.Create = dr.Create;
                    _database.Delete = dr.Delete;
                    _database.Read = dr.Read;

                    if (_database.Change || _database.Create || _database.Delete || _database.Read)
                    {
                        foreach (var t in _database.Tables)
                        {
                            var tr = _rights.FirstOrDefault(x => x.ForeignKey == $"{_database.DatabaseId}{t.TableId}");
                            if (tr is not null)
                            {
                                t.Change = tr.Change;
                                t.Create = tr.Create;
                                t.Delete = tr.Delete;
                                t.Read = tr.Read;

                                if (tr.Change || tr.Create || tr.Delete || tr.Read)
                                {
                                    foreach (var f in t.Fields)
                                    {
                                        var fr = _rights.FirstOrDefault(x => x.ForeignKey == $"{_database.DatabaseId}{t.TableId}{f.FieldId}");
                                        if (fr is not null)
                                        {
                                            f.Change = fr.Change;
                                            f.Create = fr.Create;
                                            f.Delete = fr.Delete;
                                            f.Read = fr.Read;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void SelectTable(GrpcService.Database.DatabaseTable? table)
        {
            if (table is null)
            {
                _selectTableValue = "";
                _databaseTable = null;
                _fields = null;
                return;
            }

            _databaseTable = table;
            _fields = table.Fields.AsQueryable();
        }

        private void DatabaseRightChangeClick(bool val)
        {
            if (_database is not null)
            {
                _database.Change = val;
                foreach (var t in _database.Tables)
                {
                    t.Change = val;
                    foreach(var f in t.Fields)
                        f.Change = val;
                }
            }
        }

        private void DatabaseRightCreateClick(bool val)
        {
            if (_database is not null)
            {
                _database.Create = val;
                foreach (var t in _database.Tables)
                {
                    t.Create = val;
                    foreach (var f in t.Fields)
                        f.Create = val;
                }
            }
        }

        private void DatabaseRightDeleteClick(bool val)
        {
            if (_database is not null)
            {
                _database.Delete = val;
                foreach (var t in _database.Tables)
                {
                    t.Delete = val;
                    foreach (var f in t.Fields)
                        f.Delete = val;
                }
            }
        }

        private void DatabaseRightReadClick(bool val)
        {
            if (_database is not null)
            {
                _database.Read = val;
                foreach (var t in _database.Tables)
                {
                    t.Read = val;
                    foreach (var f in t.Fields)
                        f.Read = val;
                }
            }
        }

        private void DatabaseTableRightChangeClick(bool val)
        {
            if (_database is not null && _databaseTable is not null)
            {
                _database.Change = val;
                _databaseTable.Change = val;
                foreach (var f in _databaseTable.Fields)
                    f.Change = val;
            }
        }

        private void DatabaseTableRightCreateClick(bool val)
        {
            if (_database is not null && _databaseTable is not null)
            {
                _database.Create = val;
                _databaseTable.Create = val;
                foreach (var f in _databaseTable.Fields)
                    f.Create = val;
            }
        }

        private void DatabaseTableRightDeleteClick(bool val)
        {
            if (_database is not null && _databaseTable is not null)
            {
                _database.Delete = val;
                _databaseTable.Delete = val;
                foreach (var f in _databaseTable.Fields)
                    f.Delete = val;
            }
        }

        private void DatabaseTableRightReadClick(bool val)
        {
            if (_database is not null && _databaseTable is not null)
            {
                _database.Read = val;
                _databaseTable.Read = val;
                foreach (var f in _databaseTable.Fields)
                    f.Read = val;
            }
        }

        private void DatabaseTableFieldRightChangeClick(GrpcService.Database.DatabaseTableField field, bool val)
        {
            if (_database is not null && _databaseTable is not null)
            {
                _database.Change = val;
                _databaseTable.Change = val;
                field.Change = val;
            }
        }

        private void DatabaseTableFieldRightCreateClick(GrpcService.Database.DatabaseTableField field, bool val)
        {
            if (_database is not null && _databaseTable is not null)
            {
                _database.Create = val;
                _databaseTable.Create = val;
                field.Create = val;
            }
        }

        private void DatabaseTableFieldRightDeleteClick(GrpcService.Database.DatabaseTableField field, bool val)
        {
            if (_database is not null && _databaseTable is not null)
            {
                _database.Delete = val;
                _databaseTable.Delete = val;
                field.Delete = val;
            }
        }

        private void DatabaseTableFieldRightReadClick(GrpcService.Database.DatabaseTableField field, bool val)
        {
            if (_database is not null && _databaseTable is not null)
            {
                _database.Read = val;
                _databaseTable.Read = val;
                field.Read = val;
            }
        }

        private async Task Save()
        {
            if (_role is not null && _database is not null)
            {
                foreach (var r in _rights)
                    _role.Rights.Remove(r);

                if (_database.Change || _database.Create || _database.Delete || _database.Read)
                {
                    _role.Rights.Add(new GrpcService.Core.Right() {
                        ForeignKey = _database.DatabaseId,
                        Change = _database.Change,
                        Create = _database.Create,
                        Delete = _database.Delete,
                        Read = _database.Read,
                    });

                    foreach (var t in _database.Tables)
                    {
                        if (t.Change || t.Create || t.Delete || t.Read)
                        {
                            _role.Rights.Add(new GrpcService.Core.Right() {
                                ForeignKey = $"{_database.DatabaseId}{t.TableId}",
                                Change = t.Change,
                                Create = t.Create,
                                Delete = t.Delete,
                                Read = t.Read,
                            });

                            foreach (var f in t.Fields)
                            {
                                _role.Rights.Add(new GrpcService.Core.Right()
                                {
                                    ForeignKey = $"{_database.DatabaseId}{t.TableId}{f.FieldId}",
                                    Change = f.Change,
                                    Create = f.Create,
                                    Delete = f.Delete,
                                    Read = f.Read,
                                });
                            }
                        }
                    }

                    string? res;
                    try
                    {
                        await BusyDialogService.ShowBusyAsync();
                        res = await AdminService.RoleChangeAsync(_role);
                        await BusyDialogService.CloseBusyAsync();
                    }
                    catch (Exception ex)
                    {
                        res = ex.Message;
                    }

                    if (res is not null)
                    {
                        var dlgRef = await Dlg.ShowErrorAsync(res);
                        await dlgRef.Result;
                    }
                }
            }
        }
    }
}
