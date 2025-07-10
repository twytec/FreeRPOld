using FreeRP.Content;
using FreeRP.Role;
using FreeRP.ServerCore.Auth;
using FreeRP.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.ServerCore.UnitTest.Content
{
    [TestClass]
    public class Role_Add_Change_Delete_Directory
    {
        private readonly FrpDataService _data = MySettings.FrpDataService;
        private readonly FrpAuthService _admin = MySettings.TestAdmin;

        private FrpAuthService _user = default!;

        [TestInitialize]
        public async Task Init()
        {
            //Add user
            FrpUser user = new() { Email = "RoleAddChangeDeleteDirectory@test.org", Password = "TestPass123!" };
            var res = await _data.FrpUserService.AddUserAsync(user, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            FrpRole role = new() { Name = "RoleAddChangeDeleteDirectory" };
            res = await _data.FrpRoleService.AddRoleAsync(role, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            _user = new(_data, MySettings.FrpSettings, new()) { User = user };
            _user.Roles.Add(role);

            //Add access
            FrpPermission ac = new()
            {
                MemberIdKind = MemberIdKind.Role,
                AccessUri = "file://",
                AccessUriScheme = AccessUriScheme.Content,
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
        }

        [TestMethod]
        public async Task Test()
        {
            //Create directory
            FrpContentUriRequest req = new() { Uri = "file://RoleAddChangeDeleteDirectory" };
            var res = await _data.FrpContentService.CreateDirectoryAsync(req, _user);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            //Move directory
            FrpMoveContentUriRequest c = new() { SourceUri = req.Uri, DestUri = "file://RoleAddChangeDeleteDirectory2" };
            res = await _data.FrpContentService.MoveDirectoryAsync(c, _user);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);
            req.Uri = c.DestUri;

            //Delete directory
            res = await _data.FrpContentService.DeleteDirectoryAsync(req, _user);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);
        }
    }
}
