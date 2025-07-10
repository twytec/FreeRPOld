using FreeRP.Database;
using FreeRP.FrpServices;
using FreeRP.Role;
using FreeRP.ServerCore.Auth;
using FreeRP.User;

namespace FreeRP.ServerCore.UnitTest.Database
{
    [TestClass]
    public class Role_Add_Change_Delete_Database_Item
    {
        private readonly FrpDataService _data = MySettings.FrpDataService;
        private readonly FrpAuthService _admin = MySettings.TestAdmin;

        private FrpAuthService _user = default!;
        private FrpDatabaseAccess _db = default!;

        private class TestModel
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }

        [TestInitialize]
        public async Task Init()
        {
            FrpDatabase db = new()
            {
                DatabaseId = "AddChangeDeleteDatabaseItemRole",
                AccessMode = FrpAccessMode.AccessModeRole,
                AllowUnknownData = true
            };

            //Add user
            FrpUser user = new() { Email = "AddChangeDeleteDatabaseItemRole@test.org", Password = "TestPass123!" };
            var res = await _data.FrpUserService.AddUserAsync(user, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            //Add role
            FrpRole role = new() { Name = "AddChangeDeleteDatabaseItemRole" };
            res = await _data.FrpRoleService.AddRoleAsync(role, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            //Add user to role
            res = await _data.FrpRoleService.AddUserToRoleAsync(new FrpUserInRole() { RoleId = role.RoleId, UserId = user.UserId }, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            _user = new(_data, MySettings.FrpSettings, new()) { User = user };
            _user.Roles.Add(role);

            //Add database
            res = await _data.FrpDatabaseService.AddDatabaseAsync(db, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            //Add access
            FrpPermission ac = new()
            {
                MemberIdKind = MemberIdKind.Role,
                AccessUri = $"db://{db.DatabaseId}",
                AccessUriScheme = AccessUriScheme.Database,
                PermissionValues = new()
                {
                    Change = FrpPermissionValue.Allow,
                    Add = FrpPermissionValue.Allow,
                    Delete = FrpPermissionValue.Allow,
                    Read = FrpPermissionValue.Allow,
                },
                MemberId = role.RoleId
            };
            res = await _data.FrpPermissionService.AddPermissionAsync(ac, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            _db = new(db.DatabaseId, _data, _user);
        }

        [TestMethod]
        public async Task Test()
        {
            Assert.IsTrue(await _db.OpenDatabaseAsync());

            //Add item
            TestModel model = new();
            model = await _db.AddAsync(model);
            Assert.IsTrue(await _db.SaveChangesAsync());

            //Query by ID
            var m1 = await _db.FirstOrDefaultAsync<TestModel>(x => x.Id == model.Id);
            Assert.IsNotNull(m1);

            //Change
            model.Name = "RoleTest";
            await _db.ChangeAsync(model);
            Assert.IsTrue(await _db.SaveChangesAsync());

            //Query by name
            var m2 = await _db.FirstOrDefaultAsync<TestModel>(x => x.Name == model.Name);
            Assert.IsNotNull(m2);

            //Delete
            await _db.DeleteAsync<TestModel>(model.Id);
            Assert.IsTrue(await _db.SaveChangesAsync());

            //Close
            Assert.IsTrue(await _db.CloseDatabaseAsync());

            //Logs
            var ls = new FreeRP.Log.FrpLogFilter();
            ls.Items.Add(new FreeRP.Log.FrpLogFilterItem()
            {
                Kind = FreeRP.Log.FrpLogFilterKind.RecordId,
                Operator = FreeRP.Log.FrpLogOperator.Equals,
                Value = model.Id
            });
            var logs = await _data.FrpLogService.GetLogsAsync(ls, _admin);
            Assert.IsTrue(logs.Count() == 3);

            var del = logs.FirstOrDefault(x => x.Action == IFrpLogService.ActionDelete);
            Assert.IsNotNull(del);
            var res = await _data.FrpLogService.ResetLogAsync(del, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            await _db.OpenDatabaseAsync();
            m2 = await _db.FirstOrDefaultAsync<TestModel>(x => x.Name == model.Name);
            Assert.IsNotNull(m2);
        }
    }
}
