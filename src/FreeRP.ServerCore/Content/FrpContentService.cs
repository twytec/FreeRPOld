using FreeRP.Content;
using FreeRP.FrpServices;
using FreeRP.Helpers;
using FreeRP.ServerCore.Auth;
using FreeRP.ServerCore.Settings;
using FreeRP.Settings;
using Google.Protobuf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO.Compression;

namespace FreeRP.ServerCore.Content
{
    public sealed class FrpContentService : IFrpContentService, IAsyncDisposable
    {
        public ConcurrentDictionary<string, StreamingData> OpenReadFiles { get; set; } = [];
        public ConcurrentDictionary<string, StreamingData> OpenWriteFiles { get; set; } = [];
        public ConcurrentDictionary<string, FrpPermission> ContentAccess { get; set; } = new();

        private const int OuterTypeSize = 512;
        private readonly IWebHostEnvironment _env;
        private readonly IFrpDataService _ds;
        private readonly IFrpLogService _logService;
        private readonly ILogger _log;
        private readonly FrpSettings _frpSettings;
        private readonly IFrpAuthService _systemUser;

        public FrpContentService(IWebHostEnvironment env, IFrpDataService ds, IFrpLogService logService, ILogger logger, FrpSettings frpSettings)
        {
            _env = env;
            _ds = ds;
            _logService = logService;
            _log = logger;
            _frpSettings = frpSettings;

            _systemUser = new FrpAuthService(ds, frpSettings, new()) { IsAdmin = true, User = _frpSettings.System };
        }

        public async ValueTask<FrpResponse> GetContentItemsAsync(FrpContentUriRequest request, IFrpAuthService authService)
        {
            try
            {
                if (FrpUri.TryCreate(request.Uri, out var uri) == false)
                    return FrpResponse.Create(FrpErrorType.ErrorUriSchemeNotSupported, authService.I18n);

                var path = uri.ToAbsolutPath(_frpSettings.ContentSettings.ContentRootPath);
                if (Directory.Exists(path) == false)
                    return FrpResponse.Create(FrpErrorType.ErrorDirectoryNotExist, authService.I18n);

                if (authService.IsAdmin || await AllowReadAsync(uri, authService))
                {
                    string strUri = uri.GetUriAsString();

                    FrpContentItems tree = new();

                    if (uri.TryGetParent(out var p))
                    {
                        tree.Items.Add(new FrpContentItem() { 
                            Name = "...",
                            Uri = p.GetUriAsString(),
                        });
                    }

                    var di = new DirectoryInfo(path);
                    var dirs = di.GetDirectories();
                    if (dirs is not null && dirs.Length > 0)
                    {
                        foreach (var dir in dirs)
                        {
                            tree.Items.Add(new FrpContentItem()
                            {
                                Uri = $"{strUri}/{dir.Name}/",
                                Name = dir.Name,
                                Create = FrpUtcDateTime.FromDateTime(dir.CreationTimeUtc),
                                Change = FrpUtcDateTime.FromDateTime(dir.LastWriteTimeUtc)
                            });
                        }
                    }

                    var files = di.GetFiles();
                    if (files is not null && files.Length > 0)
                    {
                        foreach (var f in files)
                        {
                            tree.Items.Add(new FrpContentItem()
                            {
                                Uri = $"{strUri}/{f.Name}/",
                                Name = f.Name,
                                Create = FrpUtcDateTime.FromDateTime(f.CreationTimeUtc),
                                Change = FrpUtcDateTime.FromDateTime(f.LastWriteTimeUtc),
                                Size = (ulong)f.Length,
                                IsFile = true
                            });
                        }
                    }

                    return FrpResponse.Create(tree);
                }
                else
                    return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(GetContentItemsAsync), ex, authService);
                _log.LogError(ex, nameof(GetContentItemsAsync));

                return FrpResponse.Create(FrpErrorType.ErrorUnknown, authService.I18n);
            }
        }

        #region Download

        public async ValueTask<FrpResponse> DownloadAsync(FrpContentUriRequest request, IFrpAuthService authService)
        {
            try
            {
                if (FrpUri.TryCreate(request.Uri, out var uri) == false)
                    return FrpResponse.Create(FrpErrorType.ErrorUriSchemeNotSupported, authService.I18n);

                var downPath = Path.Combine(_env.WebRootPath, IFrpContentService.DownloadFolderName);
                if (Directory.Exists(downPath) == false)
                    Directory.CreateDirectory(downPath);

                if (await AllowReadAsync(uri, authService) == false)
                    return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

                string? downFile;

                var path = uri.ToAbsolutPath(_frpSettings.ContentSettings.ContentRootPath);
                if (File.Exists(path))
                {
                    downFile = $"{Guid.NewGuid()}{Path.GetExtension(path)}";
                    File.Copy(path, Path.Combine(downPath, downFile), true);
                }
                else if (Directory.Exists(path))
                {
                    downFile = $"{Guid.NewGuid()}.zip";
                    ZipFile.CreateFromDirectory(path, Path.Combine(downPath, downFile));
                }
                else
                {
                    return FrpResponse.Create(FrpErrorType.ErrorPathNotExist, authService.I18n);
                }

                if (downFile is not null)
                {
                    return FrpResponse.Create($"/{IFrpContentService.DownloadFolderName}/{downFile}");
                }

                return FrpResponse.Create(FrpErrorType.ErrorUnknown, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(DownloadAsync), ex, authService);
                _log.LogError(ex, nameof(DownloadAsync));

                return FrpResponse.ErrorUnknown(ex.Message);
            }
        }

        #endregion

        #region Directory

        public async ValueTask<FrpResponse> CreateDirectoryAsync(FrpContentUriRequest request, IFrpAuthService authService)
        {
            try
            {
                if (FrpUri.TryCreate(request.Uri, out var uri) == false)
                    return FrpResponse.Create(FrpErrorType.ErrorUriSchemeNotSupported, authService.I18n);

                var path = uri.ToAbsolutPath(_frpSettings.ContentSettings.ContentRootPath);
                if (Directory.Exists(path))
                    return FrpResponse.Create(FrpErrorType.ErrorDirectoryExist, authService.I18n);

                if (await AllowAddAsync(uri, authService))
                {
                    Directory.CreateDirectory(path);
                    return FrpResponse.ErrorNone();
                }

                return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(CreateDirectoryAsync), ex, authService);
                _log.LogError(ex, nameof(CreateDirectoryAsync));

                return FrpResponse.ErrorUnknown(ex.Message);
            }

        }

        public async ValueTask<FrpResponse> MoveDirectoryAsync(FrpMoveContentUriRequest request, IFrpAuthService authService)
        {
            try
            {
                if (FrpUri.TryCreate(request.SourceUri, out var sourceUri) == false)
                    return FrpResponse.Create(FrpErrorType.ErrorUriSchemeNotSupported, authService.I18n);

                if (FrpUri.TryCreate(request.DestUri, out var destUri) == false)
                    return FrpResponse.Create(FrpErrorType.ErrorUriSchemeNotSupported, authService.I18n);

                var sourcePath = sourceUri.ToAbsolutPath(_frpSettings.ContentSettings.ContentRootPath);
                if (Directory.Exists(sourcePath) == false)
                    return FrpResponse.Create(FrpErrorType.ErrorDirectoryNotExist, authService.I18n);

                var destPath = destUri.ToAbsolutPath(_frpSettings.ContentSettings.ContentRootPath);
                if (Directory.Exists(destPath) && request.Replace == false)
                    return FrpResponse.Create(FrpErrorType.ErrorDirectoryExist, authService.I18n);

                if (await AllowReadAsync(sourceUri, authService) && await AllowAddAsync(sourceUri, authService))
                {
                    if (request.Copy)
                        FileSystem.CopyAll(sourcePath, destPath);
                    else
                        Directory.Move(sourcePath, destPath);

                    return FrpResponse.ErrorNone();
                }

                return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(MoveDirectoryAsync), ex, authService);
                _log.LogError(ex, nameof(MoveDirectoryAsync));

                return FrpResponse.ErrorUnknown(ex.Message);
            }
        }

        public async ValueTask<FrpResponse> DeleteDirectoryAsync(FrpContentUriRequest request, IFrpAuthService authService)
        {
            try
            {
                if (FrpUri.TryCreate(request.Uri, out var uri) == false)
                    return FrpResponse.Create(FrpErrorType.ErrorUriSchemeNotSupported, authService.I18n);

                var path = uri.ToAbsolutPath(_frpSettings.ContentSettings.ContentRootPath);
                if (Directory.Exists(path) == false)
                    return FrpResponse.Create(FrpErrorType.ErrorDirectoryNotExist, authService.I18n);

                if (await AllowDeleteAsync(uri, authService))
                {
                    DirectoryInfo di = new(path);
                    var id = Guid.NewGuid().ToString();
                    var dp = Path.Combine(_frpSettings.ContentSettings.BinRootPath, id);
                    Directory.CreateDirectory(dp);

                    Directory.Move(path, Path.Combine(dp, di.Name));

                    return FrpResponse.ErrorNone();
                }

                return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(DeleteDirectoryAsync), ex, authService);
                _log.LogError(ex, nameof(DeleteDirectoryAsync));

                return FrpResponse.ErrorUnknown(ex.Message);
            }

        }

        #endregion

        #region File

        public async ValueTask<FrpContentStream> CreateFileAsync(FrpContentUriRequest request, IFrpAuthService authService)
        {
            try
            {
                if (FrpUri.TryCreate(request.Uri, out var uri) == false)
                    return new FrpContentStream()
                    {
                        ErrorType = FrpErrorType.ErrorUriSchemeNotSupported,
                        Message = authService.I18n.Text.UriSchemeNotSupported
                    };

                bool allow = false;

                var path = uri.ToAbsolutPath(_frpSettings.ContentSettings.ContentRootPath);
                if (Path.Exists(path))
                {
                    if (request.Replace == false)
                        return new FrpContentStream() { ErrorType = FrpErrorType.ErrorFileExist };

                    if (await AllowChangeAsync(uri, authService))
                    {
                        allow = true;
                    }
                }
                else if (await AllowAddAsync(uri, authService))
                {
                    allow = true;
                }

                if (allow)
                {
                    var stream = File.Create(path);
                    string id = AddFileWrite(stream, path);

                    return new FrpContentStream() { ErrorType = FrpErrorType.ErrorNone, Id = id };
                }

                return new FrpContentStream() { ErrorType = FrpErrorType.ErrorAccessDenied, Message = authService.I18n.Text.AccessDenied };
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(CreateFileAsync), ex, authService);
                _log.LogError(ex, nameof(CreateFileAsync));

                return new FrpContentStream() { ErrorType = FrpErrorType.ErrorUnknown, Message = ex.Message };
            }
        }

        public async ValueTask<FrpContentStream> FileStreamWriteAsync(FrpContentStream request)
        {
            try
            {
                if (OpenWriteFiles.TryGetValue(request.Id, out StreamingData? value))
                {
                    if (value.Stream is not null)
                    {
                        if (request.Data.IsEmpty == false)
                            await value.Stream.WriteAsync(request.Data.Memory);

                        if (request.EOF)
                        {
                            OpenWriteFiles.TryRemove(request.Id, out _);
                            await value.Stream.DisposeAsync();
                        }
                        else
                        {
                            value.Deadline = DateTime.UtcNow.AddMinutes(_frpSettings.GrpcSettings.GrpcTransportTimeoutInMinutes);
                        }

                        return new FrpContentStream() { ErrorType = FrpErrorType.ErrorNone, Id = request.Id, EOF = request.EOF };
                    }
                    else
                    {
                        OpenWriteFiles.TryRemove(request.Id, out _);
                    }
                }

                return new FrpContentStream() { ErrorType = FrpErrorType.ErrorFileNotExist };
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(FileStreamWriteAsync), ex, _systemUser);
                _log.LogError(ex, nameof(FileStreamWriteAsync));
                return new FrpContentStream() { ErrorType = FrpErrorType.ErrorUnknown, Message = ex.Message };
            }
        }

        public async ValueTask<FrpContentStream> OpenFileReadAsync(FrpContentUriRequest request, IFrpAuthService authService)
        {
            try
            {
                if (FrpUri.TryCreate(request.Uri, out var uri) == false)
                    return new FrpContentStream()
                    {
                        ErrorType = FrpErrorType.ErrorUriSchemeNotSupported,
                        Message = authService.I18n.Text.UriSchemeNotSupported
                    };

                var path = uri.ToAbsolutPath(_frpSettings.ContentSettings.ContentRootPath);
                if (File.Exists(path))
                {
                    if (await AllowReadAsync(uri, authService))
                    {
                        try
                        {
                            var stream = File.OpenRead(path);
                            string id = AddFileRead(stream, path);

                            return new FrpContentStream() { ErrorType = FrpErrorType.ErrorNone, Id = id };
                        }
                        catch (Exception)
                        {
                            return new FrpContentStream() { ErrorType = FrpErrorType.ErrorUnknown };
                        }
                    }
                    else
                    {
                        return new FrpContentStream() { ErrorType = FrpErrorType.ErrorAccessDenied };
                    }
                }
                else
                {
                    return new FrpContentStream() { ErrorType = FrpErrorType.ErrorFileNotExist };
                }
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(OpenFileReadAsync), ex, authService);
                _log.LogError(ex, nameof(OpenFileReadAsync));

                return new FrpContentStream() { ErrorType = FrpErrorType.ErrorUnknown, Message = ex.Message };
            }
        }

        public async ValueTask<FrpContentStream> FileStreamReadAsync(FrpContentStream request)
        {
            try
            {
                if (OpenReadFiles.TryGetValue(request.Id, out StreamingData? value))
                {
                    if (value.Stream is not null)
                    {
                        FrpContentStream msg = new() { Id = request.Id };
                        var buffer = new byte[_frpSettings.GrpcSettings.GrpcMessageSizeInByte - OuterTypeSize];
                        var count = await value.Stream.ReadAsync(buffer);
                        if (count < buffer.Length)
                        {
                            msg.EOF = true;
                            OpenReadFiles.TryRemove(request.Id, out _);
                            await value.Stream.DisposeAsync();
                        }
                        else
                        {
                            value.Deadline = DateTime.UtcNow.AddMinutes(_frpSettings.GrpcSettings.GrpcTransportTimeoutInMinutes);
                        }

                        if (count > 0)
                        {
                            msg.Data = UnsafeByteOperations.UnsafeWrap(buffer.AsMemory(0, count));
                        }

                        msg.ErrorType = FrpErrorType.ErrorNone;
                        return msg;
                    }
                    else
                    {
                        OpenReadFiles.TryRemove(request.Id, out _);
                    }
                }

                return new FrpContentStream() { ErrorType = FrpErrorType.ErrorFileNotExist, EOF = true };
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(FileStreamReadAsync), ex, _systemUser);
                _log.LogError(ex, nameof(FileStreamReadAsync));

                return new FrpContentStream() { ErrorType = FrpErrorType.ErrorUnknown, EOF = true, Message = ex.Message };
            }
        }

        public async ValueTask<FrpResponse> MoveFileAsync(FrpMoveContentUriRequest request, IFrpAuthService authService)
        {
            try
            {
                if (FrpUri.TryCreate(request.SourceUri, out var sourceUri) == false)
                    return FrpResponse.Create(FrpErrorType.ErrorUriSchemeNotSupported, authService.I18n);

                if (FrpUri.TryCreate(request.DestUri, out var destUri) == false)
                    return FrpResponse.Create(FrpErrorType.ErrorUriSchemeNotSupported, authService.I18n);

                var sourcePath = sourceUri.ToAbsolutPath(_frpSettings.ContentSettings.ContentRootPath);
                if (File.Exists(sourcePath) == false)
                    return FrpResponse.Create(FrpErrorType.ErrorFileNotExist, authService.I18n);

                var destPath = destUri.ToAbsolutPath(_frpSettings.ContentSettings.ContentRootPath);
                if (File.Exists(destPath) && request.Replace == false)
                    return FrpResponse.Create(FrpErrorType.ErrorFileExist, authService.I18n);

                if (await AllowReadAsync(sourceUri, authService) && await AllowAddAsync(destUri, authService))
                {
                    if (request.Copy || request.Duplicate)
                        File.Copy(sourcePath, destPath, true);
                    else
                        File.Move(sourcePath, destPath, true);

                    return FrpResponse.ErrorNone();
                }

                return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(MoveFileAsync), ex, authService);
                _log.LogError(ex, nameof(MoveFileAsync));

                return FrpResponse.ErrorUnknown(ex.Message);
            }
        }

        public async ValueTask<FrpResponse> DeleteFileAsync(FrpContentUriRequest request, IFrpAuthService authService)
        {
            try
            {
                if (FrpUri.TryCreate(request.Uri, out var uri) == false)
                    return FrpResponse.Create(FrpErrorType.ErrorUriSchemeNotSupported, authService.I18n);

                var path = uri.ToAbsolutPath(_frpSettings.ContentSettings.ContentRootPath);
                if (File.Exists(path) == false)
                    return FrpResponse.Create(FrpErrorType.ErrorFileNotExist, authService.I18n);

                if (await AllowDeleteAsync(uri, authService))
                {
                    var id = Guid.NewGuid().ToString();
                    var dp = Path.Combine(_frpSettings.ContentSettings.BinRootPath, id);
                    Directory.CreateDirectory(dp);

                    File.Move(path, Path.Combine(dp, Path.GetFileName(path)));

                    return FrpResponse.ErrorNone();
                }

                return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(DeleteFileAsync), ex, authService);
                _log.LogError(ex, nameof(DeleteFileAsync));

                return FrpResponse.ErrorUnknown(ex.Message);
            }
        }

        #endregion

        #region Helpers

        private async ValueTask<bool> AllowAddAsync(FrpUri uri, IFrpAuthService authService)
        {
            if (authService.IsAdmin)
                return true;

            var p = await _ds.FrpPermissionService.GetUserContentPermissionAsync(uri, authService);
            return p.PermissionValues.Add == FrpPermissionValue.Allow;
        }

        private async ValueTask<bool> AllowChangeAsync(FrpUri uri, IFrpAuthService authService)
        {
            if (authService.IsAdmin)
                return true;

            var p = await _ds.FrpPermissionService.GetUserContentPermissionAsync(uri, authService);
            return p.PermissionValues.Change == FrpPermissionValue.Allow;
        }

        private async ValueTask<bool> AllowDeleteAsync(FrpUri uri, IFrpAuthService authService)
        {
            if (authService.IsAdmin)
                return true;

            var p = await _ds.FrpPermissionService.GetUserContentPermissionAsync(uri, authService);
            return p.PermissionValues.Delete == FrpPermissionValue.Allow;
        }

        private async ValueTask<bool> AllowReadAsync(FrpUri uri, IFrpAuthService authService)
        {
            if (authService.IsAdmin)
                return true;

            var p = await _ds.FrpPermissionService.GetUserContentPermissionAsync(uri, authService);
            return p.PermissionValues.Read == FrpPermissionValue.Allow;
        }

        #endregion

        #region Worker

        private bool _isRun = false;

        public string AddFileRead(Stream stream, string file)
        {
            string id = Guid.NewGuid().ToString();
            OpenReadFiles[id] = new StreamingData()
            {
                Stream = stream,
                File = file,
                Deadline = DateTime.UtcNow.AddMinutes(_frpSettings.GrpcSettings.GrpcTransportTimeoutInMinutes)
            };
            Task.Run(DeadLineWorker);

            return id;
        }

        public string AddFileWrite(Stream stream, string file)
        {
            string id = Guid.NewGuid().ToString();
            OpenWriteFiles[id] = new StreamingData()
            {
                Stream = stream,
                File = file,
                Deadline = DateTime.UtcNow.AddMinutes(_frpSettings.GrpcSettings.GrpcTransportTimeoutInMinutes)
            };
            Task.Run(DeadLineWorker);
            return id;
        }

        private async void DeadLineWorker()
        {
            if (_isRun)
                return;

            _isRun = true;

            while (_isRun)
            {
                await Task.Delay(1000);

                foreach (var item in OpenReadFiles)
                {
                    if (DateTime.UtcNow > item.Value.Deadline)
                    {
                        if (item.Value.Stream is not null)
                        {
                            await item.Value.Stream.DisposeAsync();
                        }

                        OpenReadFiles.TryRemove(item);
                    }
                }

                foreach (var item in OpenWriteFiles)
                {
                    if (DateTime.UtcNow > item.Value.Deadline)
                    {
                        if (item.Value.Stream is not null)
                        {
                            await item.Value.Stream.DisposeAsync();
                        }

                        try
                        {
                            if (File.Exists(item.Value.File))
                                File.Delete(item.Value.File);

                            OpenWriteFiles.TryRemove(item);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                if (OpenReadFiles.IsEmpty && OpenWriteFiles.IsEmpty)
                    _isRun = false;
            }
        }



        #endregion

        private async ValueTask AddExceptionAsync(string location, Exception ex, IFrpAuthService authService)
        {
            await _logService.AddExceptionLogAsync(new()
            {
                UtcUnixTimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Location = $"{nameof(FrpContentService)}/{location}",
                Message = ex.Message,
                UserId = authService.User.UserId,
                Val1 = ex.StackTrace
            }, authService);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                var down = Path.Combine(_env.WebRootPath, IFrpContentService.DownloadFolderName);
                if (Directory.Exists(down))
                {
                    Directory.Delete(down, true);
                }
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(DisposeAsync), ex, _systemUser);
                _log.LogError(ex, nameof(DisposeAsync));
            }
        }
    }
}
