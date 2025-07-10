using FreeRP.Net.Client.Translation;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace FreeRP.Net.Client.Blazor.Pages.Admin
{
    public partial class Databases : IDisposable
    {
        [Inject] private GrpcServices.AdminService AdminService { get; set; } = default!;
        [Inject] private IDialogService Dlg { get; set; } = default!;
        [Inject] private Dialog.BusyDialogService BusyDialogService { get; set; } = default!;
        [Inject] private I18nService I18n { get; set; } = default!;

        private readonly DialogParameters para = [];
        private GrpcService.Database.Database? _database;
        private GrpcService.Database.DatabaseTable? _databaseTable;

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
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            I18n.TextChanged -= UpdateUI;
            AdminService.AdminDataChanged -= AdminDataChanged;
            GC.SuppressFinalize(this);
        }

        private void SelectDatabase(GrpcService.Database.Database? db)
        {
            _database = db;

            if (_database is null)
            {
                SelectTable(null);
            }
        }

        private void SelectTable(GrpcService.Database.DatabaseTable? table)
        {
            if (table is null)
            {
                _databaseTable = null;
                _fields = null;
                return;
            }

            _databaseTable = table;
            _fields = table.Fields.AsQueryable();
        }

        private async Task AddDatabase()
        {
            var dlgRef = await Dlg.ShowDialogAsync<Dialog.EditDatabaseDialog>(new GrpcService.Database.Database(), para);
            await dlgRef.Result;
        }

        private async Task ChangeDatabase()
        {
            if (_database is not null)
            {
                var dlgRef = await Dlg.ShowDialogAsync<Dialog.EditDatabaseDialog>(_database, para);
                await dlgRef.Result;
            }
        }

        private async Task AddTable()
        {
            if (_database is not null)
            {
                Tuple<GrpcService.Database.Database, GrpcService.Database.DatabaseTable> data = new(_database, new());
                var dlgRef = await Dlg.ShowDialogAsync<Dialog.EditDatabaseTableDialog>(data, para);
                await dlgRef.Result;
            }
        }

        private async Task ChangeTable()
        {
            if (_database is not null && _databaseTable is not null)
            {
                Tuple<GrpcService.Database.Database, GrpcService.Database.DatabaseTable> data = new(_database, _databaseTable);
                var dlgRef = await Dlg.ShowDialogAsync<Dialog.EditDatabaseTableDialog>(data, para);
                await dlgRef.Result;
            }
        }

        private async Task AddField()
        {
            if (_database is not null && _databaseTable is not null)
            {
                Tuple<GrpcService.Database.Database, GrpcService.Database.DatabaseTable, GrpcService.Database.DatabaseTableField> data = new(_database, _databaseTable, new());
                var dlgRef = await Dlg.ShowDialogAsync<Dialog.EditDatabseTableFieldDialog>(data, para);
                var res = await dlgRef.Result;
                if (res.Cancelled == false)
                {
                    _fields = _databaseTable.Fields.AsQueryable();
                }
            }
        }

        private async Task ChangeField(GrpcService.Database.DatabaseTableField field)
        {
            if (_database is not null && _databaseTable is not null)
            {
                Tuple<GrpcService.Database.Database, GrpcService.Database.DatabaseTable, GrpcService.Database.DatabaseTableField> data = new(_database, _databaseTable, field);
                var dlgRef = await Dlg.ShowDialogAsync<Dialog.EditDatabseTableFieldDialog>(data, para);
                var res = await dlgRef.Result;
                if (res.Cancelled == false)
                {
                    _fields = _databaseTable.Fields.AsQueryable();
                }
            }
        }
    }
}
