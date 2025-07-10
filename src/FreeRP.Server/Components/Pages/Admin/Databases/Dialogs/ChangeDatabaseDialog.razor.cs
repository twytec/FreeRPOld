using FreeRP.Database;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FreeRP.Server.Components.Pages.Admin.Databases.Dialogs
{
    public partial class ChangeDatabaseDialog
    {
        [CascadingParameter]
        public MudDialogInstance Dialog { get; set; } = default!;

        [Parameter]
        public FrpDatabase Content { get; set; } = default!;

        void Cancel() => Dialog.Cancel();
        void Save() => Dialog.Close();

        #region DataStructure

        private record DataStruct(string Id, FrpDataField Field, FrpDataField? Parent = null);
        private FrpDataset? _selectedDataStructure;
        private readonly List<DataStruct> _dataFields = [];

        private void DataSetSelectChange(string id)
        {
            _selectedDataStructure = Content.Datasets.FirstOrDefault(x => x.DatasetId == id);
            LoadDataFields();
        }

        private void LoadDataFields()
        {
            if (_selectedDataStructure == null)
                return;

            _dataFields.Clear();
            foreach (var item in _selectedDataStructure.Fields)
            {
                _dataFields.Add(new DataStruct(item.GetName(), item));

                if (item.DataType is FrpDatabaseDataType.FieldObject && item.Fields.Count > 0)
                {
                    LoadDataFieldChilds(item);
                }
            }
        }

        private void LoadDataFieldChilds(FrpDataField field)
        {
            foreach (var item in field.Fields)
            {
                _dataFields.Add(new DataStruct($"{field.GetName()}/{item.GetName()}", item, field));

                if (item.DataType is FrpDatabaseDataType.FieldObject && item.Fields.Count > 0)
                {
                    LoadDataFieldChilds(item);
                }
            }
        }

        private async Task AddDataStructure()
        {
            FrpDataset table = new();
            var para = new DialogParameters<ChangeDataStructureDialog>
            {
                { x => x.Dataset, table }
            };

            while (true)
            {
                var dialog = dlg.Show<ChangeDataStructureDialog>(i18n.Text.DataStructure, para);
                var res = await dialog.Result;

                if (res is not null && res.Canceled)
                {
                    break;
                }
                else
                {
                    if (Content.Datasets.FirstOrDefault(x => x.DatasetId == table.DatasetId) is null)
                    {
                        Content.Datasets.Add(table);
                        LoadDataFields();
                        break;
                    }
                    else
                    {
                        await dlg.ShowMessageBox(i18n.Text.Error, i18n.Text.ErrorDataStructureExist, i18n.Text.Ok);
                    }
                }
            }
        }

        private async Task ChangeDataStructure()
        {
            if (_selectedDataStructure == null)
                return;

            var para = new DialogParameters<ChangeDataStructureDialog>
                {
                    { x => x.Dataset, _selectedDataStructure }
                };

            await dlg.Show<ChangeDataStructureDialog>(i18n.Text.Dataset, para).Result;
            LoadDataFields();
        }

        private async Task DeleteDataStructure()
        {
            if (_selectedDataStructure == null)
                return;

            var res = await dlg.ShowMessageBox(_selectedDataStructure.GetName(), i18n.Text.ReallyDelete, i18n.Text.Yes, i18n.Text.No);
            if (res == true)
            {
                Content.Datasets.Remove(_selectedDataStructure);
                LoadDataFields();
            }
        }

        private async Task AddField()
        {
            if (_selectedDataStructure == null)
                return;

            FrpDataField field = new();
            var para = new DialogParameters<ChangeDataFieldDialog>
            {
                { x => x.Field, field }
            };

            while (true)
            {
                var dialog = dlg.Show<ChangeDataFieldDialog>(i18n.Text.DataField, para);
                var res = await dialog.Result;

                if (res is not null && res.Canceled)
                {
                    break;
                }
                else
                {
                    if (_selectedDataStructure.Fields.FirstOrDefault(x => x.FieldId == field.FieldId) is null)
                    {
                        _selectedDataStructure.Fields.Add(field);
                        LoadDataFields();
                        break;
                    }
                    else
                    {
                        await dlg.ShowMessageBox(i18n.Text.Error, i18n.Text.ErrorDataFieldExist, i18n.Text.Ok);
                    }
                }
            }
        }

        private async Task AddFieldToObject(DataStruct s)
        {
            if (_selectedDataStructure == null)
                return;

            FrpDataField field = new();
            var para = new DialogParameters<ChangeDataFieldDialog>
            {
                { x => x.Field, field }
            };

            while (true)
            {
                var dialog = dlg.Show<ChangeDataFieldDialog>(i18n.Text.DataField, para);
                var res = await dialog.Result;

                if (res is not null && res.Canceled)
                {
                    break;
                }
                else
                {
                    if (s.Field.Fields.FirstOrDefault(x => x.FieldId == field.FieldId) is null)
                    {
                        s.Field.Fields.Add(field);
                        LoadDataFields();
                        break;
                    }
                    else
                    {
                        await dlg.ShowMessageBox(i18n.Text.Error, i18n.Text.ErrorDataFieldExist, i18n.Text.Ok);
                    }
                }
            }
        }

        private async Task ChangeField(FrpDataField field)
        {
            var para = new DialogParameters<ChangeDataFieldDialog>
                {
                    { x => x.Field, field }
                };

            await dlg.Show<ChangeDataFieldDialog>(i18n.Text.DataField, para).Result;
            LoadDataFields();
        }

        private async Task DeleteField(DataStruct s)
        {
            if (_selectedDataStructure == null)
                return;

            var res = await dlg.ShowMessageBox(s.Field.GetName(), i18n.Text.ReallyDelete, i18n.Text.Yes, i18n.Text.No);
            if (res == true)
            {
                if (s.Parent is null)
                {
                    _selectedDataStructure.Fields.Remove(s.Field);
                }
                else
                {
                    s.Parent.Fields.Remove(s.Field);
                }
                LoadDataFields();
            }
        }

        #endregion
    }
}
