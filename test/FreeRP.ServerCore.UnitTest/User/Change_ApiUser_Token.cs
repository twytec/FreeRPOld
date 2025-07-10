using FreeRP.FrpServices;
using FreeRP.ServerCore.Auth;
using FreeRP.User;

namespace FreeRP.ServerCore.UnitTest.User
{
    [TestClass]
    public class Change_ApiUser_Token
    {
        private readonly FrpDataService _data = MySettings.FrpDataService;
        private readonly FrpAuthService _admin = MySettings.TestAdmin;

        FrpUser _user = default!;

        [TestInitialize]
        public async Task Init()
        {
            _user = new()
            {
                Email = "Change_ApiUser_Token@test.com",
                UtcDateTime = FrpUtcDateTime.FromDateTime(DateTime.UtcNow.AddYears(1))
            };

            //Add
            var res = await _data.FrpUserService.AddApiUserAsync(_user, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);
        }

        [TestMethod]
        public async Task Test()
        {
            var res = await _data.FrpUserService.GetApiUserTokenAsync(_user, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            var token = res.Data;

            _user.UtcDateTime = FrpUtcDateTime.FromDateTime(DateTime.UtcNow.AddYears(2));
            res = await _data.FrpUserService.ChangeApiUserTokenAsync(_user, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            //Logs
            var ls = new FreeRP.Log.FrpLogFilter();
            ls.Items.Add(new FreeRP.Log.FrpLogFilterItem()
            {
                Kind = FreeRP.Log.FrpLogFilterKind.RecordId,
                Operator = FreeRP.Log.FrpLogOperator.Equals,
                Value = _user.UserId
            });
            var logs = await _data.FrpLogService.GetLogsAsync(ls, _admin);
            var del = logs.FirstOrDefault(x => x.Action == IFrpLogService.ActionChangePasswort);
            Assert.IsNotNull(del);

            res = await _data.FrpLogService.ResetLogAsync(del, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            res = await _data.FrpUserService.GetApiUserTokenAsync(_user, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);
            Assert.IsTrue(token == res.Data);
        }
    }
}
