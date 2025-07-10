using FreeRP.Database;
using FreeRP.FrpServices;
using FreeRP.ServerCore.Auth;

namespace FreeRP.ServerCore.UnitTest.Database
{
    [TestClass]
    public class Add_Change_Delete_Database
    {
        private readonly FrpDataService _data = MySettings.FrpDataService;
        private readonly FrpAuthService _admin = MySettings.TestAdmin;

        [TestMethod]
        public async Task Test()
        {
            FrpDatabase db = new()
            {
                DatabaseProvider = DatabaseProvider.Sqlite,
                DatabaseId = "Add_Change_Delete_Database",
            };

            //Add
            var res = await _data.FrpDatabaseService.AddDatabaseAsync(db, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            var db2 = await _data.FrpDatabaseService.GetDatabaseByIdAsync(db.DatabaseId);
            Assert.IsNotNull(db2);

            //Change
            db.Datasets.Add(new FrpDataset() { DatasetId = "FooBar" });
            res = await _data.FrpDatabaseService.ChangeDatabaseAsync(db, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            db2 = await _data.FrpDatabaseService.GetDatabaseByIdAsync(db.DatabaseId);
            Assert.IsNotNull(db2);
            Assert.IsTrue(db2.Datasets.FirstOrDefault(x => x.DatasetId == "FooBar") is not null);

            //Delete
            res = await _data.FrpDatabaseService.DeleteDatabaseAsync(db, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);
            db2 = await _data.FrpDatabaseService.GetDatabaseByIdAsync(db.DatabaseId);
            Assert.IsNull(db2);

            //Logs
            var ls = new FreeRP.Log.FrpLogFilter();
            ls.Items.Add(new FreeRP.Log.FrpLogFilterItem()
            {
                Kind = FreeRP.Log.FrpLogFilterKind.RecordId,
                Operator = FreeRP.Log.FrpLogOperator.Equals,
                Value = db.DatabaseId
            });
            var logs = await _data.FrpLogService.GetLogsAsync(ls, _admin);
            Assert.IsTrue(logs.Count() == 3);

            var del = logs.FirstOrDefault(x => x.Action == IFrpLogService.ActionDelete);
            Assert.IsNotNull(del);
            res = await _data.FrpLogService.ResetLogAsync(del, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);
            db2 = await _data.FrpDatabaseService.GetDatabaseByIdAsync(db.DatabaseId);
            Assert.IsNotNull(db2);
        }
    }
}
