using FreeRP.Content;

namespace FreeRP.FrpServices
{
    public interface IFrpContentService
    {
        public const string RecordTypeContent = "FrpContent";
        public const string DownloadFolderName = "down";
        public const string UriSchemeFile = "file";

        /// <summary>
        /// Get all files and directories from path
        /// </summary>
        /// <param name="request"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> GetContentItemsAsync(FrpContentUriRequest request, IFrpAuthService authService);

        /// <summary>
        /// Download ContentItem
        /// </summary>
        /// <param name="request"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> DownloadAsync(FrpContentUriRequest request, IFrpAuthService authService);

        /// <summary>
        /// Creates a directory if it does not exist and the user has access
        /// </summary>
        /// <param name="request"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> CreateDirectoryAsync(FrpContentUriRequest request, IFrpAuthService authService);

        /// <summary>
        /// Moves the directory to new location if it exists and the user has access
        /// </summary>
        /// <param name="request"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> MoveDirectoryAsync(FrpMoveContentUriRequest request, IFrpAuthService authService);

        /// <summary>
        /// Deletes the directory if it exists and the user has access
        /// </summary>
        /// <param name="request"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> DeleteDirectoryAsync(FrpContentUriRequest request, IFrpAuthService authService);

        /// <summary>
        /// Creates or overwrites a file and open ContentStream with write access if user has access
        /// </summary>
        /// <param name="request"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpContentStream> CreateFileAsync(FrpContentUriRequest request, IFrpAuthService authService);

        /// <summary>
        /// Write data to the stream
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        ValueTask<FrpContentStream> FileStreamWriteAsync(FrpContentStream request);

        /// <summary>
        /// Open ContentStream with read access if it exists and the user has access
        /// </summary>
        /// <param name="request"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpContentStream> OpenFileReadAsync(FrpContentUriRequest request, IFrpAuthService authService);

        /// <summary>
        /// Read date from the stream
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        ValueTask<FrpContentStream> FileStreamReadAsync(FrpContentStream request);

        /// <summary>
        /// Moves the file to new location if it exists and the user has access
        /// </summary>
        /// <param name="request"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> MoveFileAsync(FrpMoveContentUriRequest request, IFrpAuthService authService);

        /// <summary>
        /// Deletes the file if it exists and the user has access
        /// </summary>
        /// <param name="request"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> DeleteFileAsync(FrpContentUriRequest request, IFrpAuthService authService);
    }
}
