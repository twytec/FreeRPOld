using FreeRP.FrpServices;

namespace FreeRP.Database
{
    public class FrpDatabasePermissions
    {
        public FrpDatabasePermissionItem Main { get; private set; }
        public List<FrpDatabasePermissionItem> Datasets { get; private set; } = [];
        public Dictionary<string, FrpDatabasePermissionItem> All { get; private set; } = [];
        public bool Loaded { get; set; }

        public bool AllAddAllowed { get; set; } = true;
        public bool AllChangeAllowed { get; set; } = true;
        public bool AllDeleteAllowed { get; set; } = true;
        public bool AllReadAllowed { get; set; } = true;

        private string? _id;
        private MemberIdKind _accessIdKind;
        private IEnumerable<FrpPermission>? _accesses;

        public FrpDatabasePermissions(FrpDatabase db)
        {
            string uri = $"{IFrpDatabaseService.UriSchemeDatabase}://{db.DatabaseId}";
            Main = new("/", uri);
            All.Add(Main.Uri, Main);

            foreach (var item in db.Datasets)
            {
                string furi = $"{uri}/{item.DatasetId}";
                string path = item.GetName();

                FrpDatabasePermissionItem ds = new(path, furi, true, false, FrpDatabaseDataType.FieldObject, Main);
                Datasets.Add(ds);
                All.Add(ds.Uri, ds);

                if (item.Fields.Count > 0)
                {
                    foreach (var f in item.Fields)
                    {
                        AddDataSetFields(f, ds);
                    }
                }
            }
        }

        private void AddDataSetFields(FrpDataField field, FrpDatabasePermissionItem parent)
        {
            string uri = $"{parent.Uri}/{field.FieldId}";
            string path = $"{parent.Path}/{field.GetName()}";

            FrpDatabasePermissionItem f = new(path, uri, false, field.IsPrimaryKey, field.DataType, parent);
            All.Add(f.Uri, f);

            if (field.Fields.Count > 0)
            {
                foreach (var item in field.Fields)
                {
                    AddDataSetFields(item, f);
                }
            }
        }

        public FrpPermissionValues? GetDatasetPermission(string datasetId)
        {
            var uri = $"{Main.Uri}/{datasetId}";
            if (All.TryGetValue(uri, out var ds))
            {
                return ds.PermissionValues;
            }

            return null;
        }

        public void SetPermission(string? id, MemberIdKind kind, IEnumerable<FrpPermission> accesses)
        {
            _id = id;
            _accessIdKind = kind;

            SetPermission(accesses);
        }

        public void SetPermission(IEnumerable<FrpPermission> accesses)
        {
            Clean();
            Loaded = true;
            _accesses = accesses.OrderBy(x => x.AccessUri);

            foreach (var item in _accesses)
            {
                if (All.TryGetValue(item.AccessUri, out var val))
                {
                    val.PermissionValues.MergeFrom(item.PermissionValues);
                    val.Permission = item;
                    val.AddSelectionChanged(val.PermissionValues.Add);
                    val.ChangeSelectionChanged(val.PermissionValues.Change);
                    val.DeleteSelectionChanged(val.PermissionValues.Delete);
                    val.ReadSelectionChanged(val.PermissionValues.Read);

                    if (val.PermissionValues.Add is not FrpPermissionValue.Allow)
                        AllAddAllowed = false;

                    if (val.PermissionValues.Change is not FrpPermissionValue.Allow)
                        AllChangeAllowed = false;

                    if (val.PermissionValues.Delete is not FrpPermissionValue.Allow)
                        AllDeleteAllowed = false;

                    if (val.PermissionValues.Read is not FrpPermissionValue.Allow)
                        AllReadAllowed = false;
                }
            }
        }

        public async ValueTask<FrpResponse?> SaveAsync(IFrpPermissionService accessService, IFrpAuthService authService)
        {
            if (_id is null)
                return null;

            //If all values are AccessValue.Undefined, delete all
            if (FrpPermissionValues.AreAll(Main.PermissionValues, FrpPermissionValue.Undefined))
            {
                foreach (var item in All)
                {
                    if (item.Value.Permission is not null)
                    {
                        var res = await accessService.DeletePermissionAsync(item.Value.Permission, authService);
                        if (res.ErrorType is not FrpErrorType.ErrorNone)
                            return res;
                    }
                }
            }
            //If all values are the same, only keep the top one
            else if (FrpPermissionValues.AreAll(Main.PermissionValues, Main.PermissionValues.Add))
            {
                FrpPermission? main = Main.Permission;
                foreach (var item in All)
                {
                    if (item.Value.Permission is not null)
                    {
                        if (item.Value.Permission == main)
                            continue;

                        var res = await accessService.DeletePermissionAsync(item.Value.Permission, authService);
                        if (res.ErrorType is not FrpErrorType.ErrorNone)
                            return res;
                    }
                }

                if (main is null)
                {
                    main = new()
                    {
                        
                        MemberIdKind = _accessIdKind,
                        AccessUri = Main.Uri,
                        AccessUriScheme = AccessUriScheme.Database,
                        PermissionValues = new(),
                        MemberId = _id
                    };
                }

                FrpPermissionValues.SetAllValues(main.PermissionValues, FrpPermissionValue.Allow);
                if (string.IsNullOrEmpty(main.MemberIdAccessUri))
                    return await accessService.AddPermissionAsync(main, authService);
                else
                    return await accessService.ChangePermissionAsync(main, authService);
            }
            else
            {
                foreach (var item in All)
                {
                    if (item.Value.Add || item.Value.Change || item.Value.Delete || item.Value.Read)
                    {
                        FrpResponse? res = null;

                        if (item.Value.Permission is not null && FrpPermissionValues.AreAll(item.Value.PermissionValues, FrpPermissionValue.Undefined))
                        {
                            res = await accessService.DeletePermissionAsync(item.Value.Permission, authService);
                        }
                        else if (FrpPermissionValues.AreAll(item.Value.PermissionValues, FrpPermissionValue.Undefined))
                        {
                            continue;
                        }
                        else if (item.Value.Permission is not null)
                        {
                            if (item.Value.Permission.PermissionValues.Equals(item.Value.PermissionValues) == false)
                            {
                                item.Value.Permission.PermissionValues.MergeFrom(item.Value.PermissionValues);
                                res = await accessService.ChangePermissionAsync(item.Value.Permission, authService);
                            }
                        }
                        else
                        {
                            item.Value.Permission = new()
                            {
                                MemberIdKind = _accessIdKind,
                                AccessUri = item.Value.Uri,
                                AccessUriScheme = AccessUriScheme.Database,
                                PermissionValues = item.Value.PermissionValues.Clone(),
                                MemberId = _id
                            };
                            res = await accessService.AddPermissionAsync(item.Value.Permission, authService);
                        }

                        if (res is not null && res.ErrorType is not FrpErrorType.ErrorNone)
                            return res;
                    }
                }
            }

            return null;
        }

        private void Clean()
        {
            foreach (var item in All)
            {
                item.Value.Clean();
            }
        }
    }

    public record FrpDatabasePermissionItem
    {
        public FrpPermissionValues PermissionValues { get; init; } = new();
        public FrpPermission? Permission { get; set; }
        public string Path { get; init; }
        public string Uri { get; init; }
        public bool IsDataset { get; init; }
        public bool IsPrimaryKey { get; init; }
        public FrpDatabaseDataType DataType { get; init; }
        public List<FrpDatabasePermissionItem> Children { get; init; } = [];
        public FrpDatabasePermissionItem? Parent { get; init; }

        public bool Add { get; set; }
        public bool Change { get; set; }
        public bool Delete { get; set; }
        public bool Read { get; set; }

        public FrpDatabasePermissionItem(
            string path, string uri,
            bool isDataSet = false, bool isPrimaryKey = false,
            FrpDatabaseDataType dataType = FrpDatabaseDataType.FieldNull,
            FrpDatabasePermissionItem? parent = null)
        {
            Path = path;
            Uri = uri;
            IsDataset = isDataSet;
            IsPrimaryKey = isPrimaryKey;
            DataType = dataType;
            Parent = parent;
            Parent?.Children.Add(this);
        }

        public void Clean()
        {
            Add = false;
            Change = false;
            Delete = false;
            Read = false;

            FrpPermissionValues.SetAllValues(PermissionValues, FrpPermissionValue.Undefined);
            Permission = null;
        }

        #region Add

        public void AddSelectionChanged(FrpPermissionValue val)
        {
            Add = true;
            AddParentChange(val);
            AddChildChange(val);
        }

        private void AddChildChange(FrpPermissionValue val)
        {
            PermissionValues.Add = val;
            if (Children.Count > 0)
            {
                foreach (var item in Children)
                {
                    item.AddChildChange(val);
                }
            }
        }

        private void AddParentChange(FrpPermissionValue val)
        {
            if (val > PermissionValues.Add ||
                Children.Where(x => x.PermissionValues.Add == PermissionValues.Add).Any() == false)
            {
                PermissionValues.Add = val;
                Parent?.AddParentChange(val);
            }
        }

        #endregion

        #region Change

        public void ChangeSelectionChanged(FrpPermissionValue val)
        {
            Change = true;
            ChangeParentChange(val);
            ChangeChildChange(val);
        }

        private void ChangeChildChange(FrpPermissionValue val)
        {
            PermissionValues.Change = val;
            if (Children.Count > 0)
            {
                foreach (var item in Children)
                {
                    item.ChangeChildChange(val);
                }
            }
        }

        private void ChangeParentChange(FrpPermissionValue val)
        {
            if (val > PermissionValues.Change ||
                Children.Where(x => x.PermissionValues.Change == PermissionValues.Change).Any() == false)
            {
                PermissionValues.Change = val;
                Parent?.ChangeParentChange(val);
            }
        }

        #endregion

        #region Delete

        public void DeleteSelectionChanged(FrpPermissionValue val)
        {
            Delete = true;
            DeleteParentChange(val);
            DeleteChildChange(val);
        }

        private void DeleteChildChange(FrpPermissionValue val)
        {
            PermissionValues.Delete = val;
            if (Children.Count > 0)
            {
                foreach (var item in Children)
                {
                    item.DeleteChildChange(val);
                }
            }
        }

        private void DeleteParentChange(FrpPermissionValue val)
        {
            if (val > PermissionValues.Delete ||
                Children.Where(x => x.PermissionValues.Delete == PermissionValues.Delete).Any() == false)
            {
                PermissionValues.Delete = val;
                Parent?.DeleteParentChange(val);
            }
        }

        #endregion

        #region Read

        public void ReadSelectionChanged(FrpPermissionValue val)
        {
            Read = true;
            ReadParentChange(val);
            ReadChildChange(val);
        }

        private void ReadChildChange(FrpPermissionValue val)
        {
            PermissionValues.Read = val;
            if (Children.Count > 0)
            {
                foreach (var item in Children)
                {
                    item.ReadChildChange(val);
                }
            }
        }

        private void ReadParentChange(FrpPermissionValue val)
        {
            if (val > PermissionValues.Read ||
                Children.Where(x => x.PermissionValues.Read == PermissionValues.Read).Any() == false)
            {
                PermissionValues.Read = val;
                Parent?.ReadParentChange(val);
            }
        }

        #endregion
    }
}
