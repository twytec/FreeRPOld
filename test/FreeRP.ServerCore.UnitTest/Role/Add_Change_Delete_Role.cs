using FreeRP.FrpServices;
using FreeRP.Role;
using FreeRP.ServerCore.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.ServerCore.UnitTest.Role
{
    [TestClass]
    public class Add_Change_Delete_Role
    {
        private readonly FrpDataService _data = MySettings.FrpDataService;
        private readonly FrpAuthService _admin = MySettings.TestAdmin;

        [TestMethod]
        public async Task Test()
        {
            FrpRole role = new()
            {
                Name = "Add_Change_Delete_Role"
            };

            //Add
            var res = await _data.FrpRoleService.AddRoleAsync(role, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            //Change
            role.Name += 2;
            res = await _data.FrpRoleService.ChangeRoleAsync(role, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            //Get
            Assert.IsTrue(await _data.FrpRoleService.IsRoleByNameExistsAsync(role.Name));

            //Delete
            res = await _data.FrpRoleService.DeleteRoleAsync(role, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            //Get
            Assert.IsFalse(await _data.FrpRoleService.IsRoleByNameExistsAsync(role.Name));

            //Logs
            var ls = new FreeRP.Log.FrpLogFilter();
            ls.Items.Add(new FreeRP.Log.FrpLogFilterItem()
            {
                Kind = FreeRP.Log.FrpLogFilterKind.RecordId,
                Operator = FreeRP.Log.FrpLogOperator.Equals,
                Value = role.RoleId
            });
            var logs = await _data.FrpLogService.GetLogsAsync(ls, _admin);
            Assert.IsTrue(logs.Count() == 3);

            var del = logs.FirstOrDefault(x => x.Action == IFrpLogService.ActionDelete);
            Assert.IsNotNull(del);
            res = await _data.FrpLogService.ResetLogAsync(del, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            //Get
            Assert.IsTrue(await _data.FrpRoleService.IsRoleByNameExistsAsync(role.Name));
        }
    }
}
