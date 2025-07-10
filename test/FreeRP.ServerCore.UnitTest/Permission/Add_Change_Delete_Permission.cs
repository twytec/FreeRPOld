using FreeRP.FrpServices;
using FreeRP.ServerCore.Auth;
using FreeRP.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.ServerCore.UnitTest.Permission
{
    [TestClass]
    public class Add_Change_Delete_Permission
    {
        private readonly FrpDataService _data = MySettings.FrpDataService;
        private readonly FrpAuthService _admin = MySettings.TestAdmin;

        private FrpAuthService _user = default!;

        [TestInitialize]
        public async Task Init()
        {
            //Add user
            FrpUser user = new() { Email = "Add_Change_Delete_Permission@test.org", Password = "TestPass123!" };
            var res = await _data.FrpUserService.AddUserAsync(user, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            _user = new(_data, MySettings.FrpSettings, new()) { User = user };
        }

        [TestMethod]
        public async Task Test()
        {
            FrpPermission ac = new()
            {
                MemberIdKind = MemberIdKind.User,
                MemberId = _user.User.UserId,
                AccessUri = "file://",
                AccessUriScheme = AccessUriScheme.Content,
                PermissionValues = new()
                {
                    Change = FrpPermissionValue.Allow,
                    Add = FrpPermissionValue.Allow,
                    Delete = FrpPermissionValue.Allow,
                    Read = FrpPermissionValue.Undefined,
                },
            };

            //Add
            var res = await _data.FrpPermissionService.AddPermissionAsync(ac, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            //Change
            ac.PermissionValues.Read = FrpPermissionValue.Allow;
            res = await _data.FrpPermissionService.ChangePermissionAsync(ac, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);
            var p = await _data.FrpPermissionService.GetPermissionByIdAsync(ac.MemberIdAccessUri);
            Assert.IsTrue(p is not null && p.PermissionValues.Read == FrpPermissionValue.Allow);

            //Delete
            res = await _data.FrpPermissionService.DeletePermissionAsync(ac, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            //Logs
            var ls = new FreeRP.Log.FrpLogFilter();
            ls.Items.Add(new FreeRP.Log.FrpLogFilterItem()
            {
                Kind = FreeRP.Log.FrpLogFilterKind.RecordId,
                Operator = FreeRP.Log.FrpLogOperator.Equals,
                Value = ac.MemberIdAccessUri
            });
            var logs = await _data.FrpLogService.GetLogsAsync(ls, _admin);
            Assert.IsTrue(logs.Count() == 3);

            var del = logs.FirstOrDefault(x => x.Action == IFrpLogService.ActionDelete);
            Assert.IsNotNull(del);
            res = await _data.FrpLogService.ResetLogAsync(del, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            p = await _data.FrpPermissionService.GetPermissionByIdAsync(ac.MemberIdAccessUri);
            Assert.IsNotNull(p);
        }
    }
}
