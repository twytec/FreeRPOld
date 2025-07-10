using FreeRP.Auth;
using FreeRP.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.ServerCore.UnitTest.Auth
{
    [TestClass]
    public class AuthUnitTest
    {
        private readonly ServerCore.Auth.FrpAuthService _frpAuthService;

        public AuthUnitTest()
        {
            _frpAuthService = new(MySettings.FrpDataService, MySettings.FrpSettings, MySettings.TestAdmin.I18n);
        }

        [TestMethod]
        public async Task Login()
        {
            var res = await _frpAuthService.LoginAsync(MySettings.FrpSettings.Admin);
            Assert.IsNotNull(res);

            Assert.IsTrue(res.IsAdmin);
            Assert.IsTrue(res.User.Password == "");
        }

        [TestMethod]
        public async Task LoginWithUnkownUser()
        {
            FrpUser tu = new()
            { 
                Email = "FooBar",
                Password = "FooBar"
            };

            var res = await _frpAuthService.LoginAsync(tu);
            Assert.IsNotNull(res);

            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorUserNotExist);
        }

        [TestMethod]
        public async Task PingServerAsync()
        {
            FrpPingData pingData = new() { AnyData = "1234" };
            var res = await _frpAuthService.PingServerAsync(pingData);
            Assert.IsNotNull(res);
            Assert.IsTrue(res.AnyData == pingData.AnyData);
        }
    }
}
