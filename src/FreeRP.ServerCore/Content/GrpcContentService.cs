using FreeRP.Content;
using FreeRP.FrpServices;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace FreeRP.ServerCore.Content
{
    public class GrpcContentService(IFrpDataService ds, IFrpAuthService auth) : FreeRP.Content.GrpcContentService.GrpcContentServiceBase
    {
        private readonly IFrpDataService _ds = ds;
        private readonly IFrpAuthService _auth = auth;

        [Authorize]
        public override async Task<FrpResponse> GetContentItems(FrpContentUriRequest request, ServerCallContext context)
        {
            return FrpResponse.Create(await _ds.FrpContentService.GetContentItemsAsync(request, _auth));
        }

        [Authorize]
        public override async Task<FrpResponse> Download(FrpContentUriRequest request, ServerCallContext context)
        {
            return await _ds.FrpContentService.DownloadAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> CreateDirectory(FrpContentUriRequest request, ServerCallContext context)
        {
            return await _ds.FrpContentService.CreateDirectoryAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> MoveDirectory(FrpMoveContentUriRequest request, ServerCallContext context)
        {
            return await _ds.FrpContentService.MoveDirectoryAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> DeleteDirectory(FrpContentUriRequest request, ServerCallContext context)
        {
            return await _ds.FrpContentService.DeleteDirectoryAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpContentStream> CreateFile(FrpContentUriRequest request, ServerCallContext context)
        {
            return await _ds.FrpContentService.CreateFileAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpContentStream> FileStreamWrite(FrpContentStream request, ServerCallContext context)
        {
            return await _ds.FrpContentService.FileStreamWriteAsync(request);
        }

        [Authorize]
        public override async Task<FrpContentStream> OpenFileRead(FrpContentUriRequest request, ServerCallContext context)
        {
            return await _ds.FrpContentService.OpenFileReadAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpContentStream> FileStreamRead(FrpContentStream request, ServerCallContext context)
        {
            return await _ds.FrpContentService.FileStreamReadAsync(request);
        }

        [Authorize]
        public override async Task<FrpResponse> MoveFileAsync(FrpMoveContentUriRequest request, ServerCallContext context)
        {
            return await _ds.FrpContentService.MoveFileAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> DeleteFile(FrpContentUriRequest request, ServerCallContext context)
        {
            return await _ds.FrpContentService.DeleteFileAsync(request, _auth);
        }
    }
}
