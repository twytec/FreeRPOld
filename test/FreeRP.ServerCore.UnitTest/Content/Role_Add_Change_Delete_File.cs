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
    public class Role_Add_Change_Delete_File
    {
        private readonly FrpDataService _data = MySettings.FrpDataService;
        private readonly FrpAuthService _admin = MySettings.TestAdmin;

        private FrpAuthService _user = default!;

        [TestInitialize]
        public async Task Init()
        {
            //Add user
            FrpUser user = new() { Email = "RoleAddChangeDeleteFile@test.org", Password = "TestPass123!" };
            var res = await _data.FrpUserService.AddUserAsync(user, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            FrpRole role = new() { Name = "RoleAddChangeDeleteFile" };
            res = await _data.FrpRoleService.AddRoleAsync(role, _admin);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            _user = new(_data, MySettings.FrpSettings, new()) { User = user };
            _user.Roles.Add(role);

            //Add access
            FrpPermission ac = new()
            {
                MemberIdKind = MemberIdKind.Role,
                AccessUri = $"file://",
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
            //Create file
            FrpContentUriRequest req = new() { Uri = "file://RoleAddChangeDeleteFile.txt" };
            var cs = await _data.FrpContentService.CreateFileAsync(req, _user);
            Assert.IsTrue(cs.ErrorType == FrpErrorType.ErrorNone);

            //Write stream
            cs.Data = Google.Protobuf.ByteString.CopyFrom("RoleAddChangeDeleteFile", Encoding.UTF8);
            cs.EOF = true;
            cs = await _data.FrpContentService.FileStreamWriteAsync(cs);
            Assert.IsTrue(cs.ErrorType == FrpErrorType.ErrorNone);

            //Open read file
            cs = await _data.FrpContentService.OpenFileReadAsync(req, _user);
            Assert.IsTrue(cs.ErrorType == FrpErrorType.ErrorNone);

            //Read stream
            cs = await _data.FrpContentService.FileStreamReadAsync(cs);
            Assert.IsTrue(cs.ErrorType == FrpErrorType.ErrorNone);
            Assert.IsTrue(cs.Data.ToStringUtf8() == "RoleAddChangeDeleteFile");

            //Move file
            FrpMoveContentUriRequest c = new() { SourceUri = req.Uri, DestUri = "file://RoleAddChangeDeleteFile2.txt" };
            var res = await _data.FrpContentService.MoveFileAsync(c, _user);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);

            //Delete file
            req.Uri = c.DestUri;
            res = await _data.FrpContentService.DeleteFileAsync(req, _user);
            Assert.IsTrue(res.ErrorType == FrpErrorType.ErrorNone);
        }
    }
}
