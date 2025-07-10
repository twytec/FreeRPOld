using FreeRP.Content;
using FreeRP.FrpServices;
using FreeRP.ServerCore;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace FreeRP.ServerGrpc
{
    public class ContentService(IFrpDataService appData, IFrpAuthService authService) : Content.ContentService.ContentServiceBase
    {
        private readonly IFrpDataService _appData = appData;
        private readonly IFrpAuthService _authService = authService;

        [Authorize]
        public override async Task<Response> DirectoryCreate(ContentUriRequest request, ServerCallContext context)
        {
            return await _appData.FrpContentService.CreateDirectoryAsync(request, _authService);
        }

        [Authorize]
        public override async Task<Response> DirectoryPathChange(ChangeContentUriRequest request, ServerCallContext context)
        {
            return await _appData.FrpContentService.MoveDirectoryAsync(request, _authService);
        }

        [Authorize]
        public override async Task<Response> DirectoryDelete(ContentUriRequest request, ServerCallContext context)
        {
            return await _appData.FrpContentService.DeleteDirectoryAsync(request, _authService);
        }

        [Authorize]
        public override async Task<ContentTreeResponse> GetContentTree(ContentUriRequest request, ServerCallContext context)
        {
            return await _appData.FrpContentService.GetContentTreeAsync(request, _authService);
        }

        [Authorize]
        public override async Task<ContentStream> FileCreate(ContentUriRequest request, ServerCallContext context)
        {
            return await _appData.FrpContentService.CreateFileAsync(request, _authService);
        }

        [Authorize]
        public override async Task<ContentStream> FileStreamWrite(ContentStream request, ServerCallContext context)
        {
            return await _appData.FrpContentService.FileStreamWriteAsync(request);
        }

        [Authorize]
        public override async Task<ContentStream> FileOpen(ContentUriRequest request, ServerCallContext context)
        {
            return await _appData.FrpContentService.OpenFileReadAsync(request, _authService);
        }

        [Authorize]
        public override async Task<ContentStream> FileStreamRead(ContentStream request, ServerCallContext context)
        {
            return await _appData.FrpContentService.FileStreamReadAsync(request);
        }

        [Authorize]
        public override async Task<Response> FilePathChange(ChangeContentUriRequest request, ServerCallContext context)
        {
            return await _appData.FrpContentService.MoveFileAsync(request, _authService);
        }

        [Authorize]
        public override async Task<Response> FileDelete(ContentUriRequest request, ServerCallContext context)
        {
            return await _appData.FrpContentService.DeleteFileAsync(request, _authService);
        }
    }
}
