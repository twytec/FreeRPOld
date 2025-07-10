using FreeRP.FrpServices;
using FreeRP.Role;
using FreeRP.ServerCore.Auth;
using FreeRP.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.ServerCore.UnitTest.Role
{
    [TestClass]
    public class Add_Delete_UserInRole
    {
        private readonly FrpDataService _data = MySettings.FrpDataService;
        private readonly FrpAuthService _admin = MySettings.TestAdmin;

        private FrpRole _frpRole = default!;
        private FrpUser _frpUser = default!;

        [TestInitialize]
        public async Task Init()
        {
            _frpRole = new()
            {
                Name = "Add_Change_Delete_UserInRole"
            };

            var res = await _data.FrpRoleService.AddRoleAsync(_frpRole, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            _frpUser = new()
            {
                Email = "Add_Change_Delete_UserInRole@test.com",
                Password = "TestPass123!"
            };

            res = await _data.FrpUserService.AddUserAsync(_frpUser, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);
        }

        [TestMethod]
        public async Task Test()
        {
            FrpUserInRole frpUserInRole = new()
            {
                RoleId = _frpRole.RoleId,
                UserId = _frpUser.UserId
            };

            //Add
            var res = await _data.FrpRoleService.AddUserToRoleAsync(frpUserInRole, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);
            Assert.IsTrue(await _data.FrpRoleService.IsUserInRoleAsync(_frpUser.UserId, _frpRole.RoleId));

            //Delete
            res = await _data.FrpRoleService.DeleteUserFromRoleAsync(frpUserInRole, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);
            Assert.IsFalse(await _data.FrpRoleService.IsUserInRoleAsync(_frpUser.UserId, _frpRole.RoleId));

            //Logs
            var ls = new FreeRP.Log.FrpLogFilter();
            ls.Items.Add(new FreeRP.Log.FrpLogFilterItem()
            {
                Kind = FreeRP.Log.FrpLogFilterKind.RecordId,
                Operator = FreeRP.Log.FrpLogOperator.Equals,
                Value = frpUserInRole.UserInRoleId
            });
            var logs = await _data.FrpLogService.GetLogsAsync(ls, _admin);
            Assert.IsTrue(logs.Count() == 2);

            var del = logs.FirstOrDefault(x => x.Action == IFrpLogService.ActionDelete);
            Assert.IsNotNull(del);
            res = await _data.FrpLogService.ResetLogAsync(del, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            Assert.IsTrue(await _data.FrpRoleService.IsUserInRoleAsync(_frpUser.UserId, _frpRole.RoleId));
        }
    }
}
