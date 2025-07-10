using FreeRP.FrpServices;
using FreeRP.ServerCore.Auth;
using FreeRP.User;

namespace FreeRP.ServerCore.UnitTest.User
{
    [TestClass]
    public class Change_User_Password
    {
        private readonly FrpDataService _data = MySettings.FrpDataService;
        private readonly FrpAuthService _admin = MySettings.TestAdmin;
        private FrpAuthService _frpAuthService = default!;

        private string _pass1 = "TestPass123!";
        private string _pass2 = "TestPass123456!";

        FrpUser _user = default!;

        [TestInitialize]
        public async Task Init()
        {
            _frpAuthService = new(MySettings.FrpDataService, MySettings.FrpSettings, MySettings.TestAdmin.I18n);

            _user = new()
            {
                Email = "Change_User_Password@test.com",
                Password = _pass1
            };

            //Add
            var res = await _data.FrpUserService.AddUserAsync(_user, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);
        }

        [TestMethod]
        public async Task Test()
        {
            _user.Password = _pass2;

            var res = await _data.FrpUserService.ChangeUserPasswordAsync(_user, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            var login = await _frpAuthService.LoginAsync(new FrpUser() { Email = _user.Email, Password = _pass2 });
            Assert.IsTrue(login.ErrorType == FrpErrorType.ErrorNone);

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

            login = await _frpAuthService.LoginAsync(new FrpUser() { Email = _user.Email, Password = _pass1 });
            Assert.IsTrue(login.ErrorType == FrpErrorType.ErrorNone);
        }
    }
}
