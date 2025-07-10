using FreeRP.FrpServices;
using FreeRP.ServerCore.Auth;
using FreeRP.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.ServerCore.UnitTest.User
{
    [TestClass]
    public class Add_Change_Delete_ApiUser
    {
        private readonly FrpDataService _data = MySettings.FrpDataService;
        private readonly FrpAuthService _admin = MySettings.TestAdmin;

        [TestMethod]
        public async Task Test()
        {
            FrpUser user = new()
            {
                Email = "Add_Change_Delete_ApiUser@test.com",
                UtcDateTime = FrpUtcDateTime.FromDateTime(DateTime.UtcNow.AddDays(1)),
            };

            //Add
            var res = await _data.FrpUserService.AddApiUserAsync(user, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            //Token
            res = await _data.FrpUserService.GetApiUserTokenAsync(user, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);
            Assert.IsNotNull(res.Data);

            //Change
            user.Email = 1 + user.Email;
            res = await _data.FrpUserService.ChangeApiUserAsync(user, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            //Get
            Assert.IsNotNull(await _data.FrpUserService.GetUserByEmailAsync(user.Email));

            //Delete
            res = await _data.FrpUserService.DeleteApiUserAsync(user, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            //Logs
            var ls = new FreeRP.Log.FrpLogFilter();
            ls.Items.Add(new FreeRP.Log.FrpLogFilterItem()
            {
                Kind = FreeRP.Log.FrpLogFilterKind.RecordId,
                Operator = FreeRP.Log.FrpLogOperator.Equals,
                Value = user.UserId
            });
            var logs = await _data.FrpLogService.GetLogsAsync(ls, _admin);
            Assert.IsTrue(logs.Count() == 3);

            var del = logs.FirstOrDefault(x => x.Action == IFrpLogService.ActionDelete);
            Assert.IsNotNull(del);
            res = await _data.FrpLogService.ResetLogAsync(del, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            //Get
            Assert.IsNotNull(await _data.FrpUserService.GetUserByEmailAsync(user.Email));
        }
    }
}
