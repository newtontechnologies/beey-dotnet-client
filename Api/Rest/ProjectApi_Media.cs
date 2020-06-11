using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Auth;
using Beey.DataExchangeModel.Projects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api.Rest
{
    /// <summary>
    /// All methods:
    /// 1) returns result if everything is ok,
    /// 2) throws exception when there is some network or framework problem,
    /// 3a) throws exception when server returned 500
    /// 3b) returns null if everything is ok, but backend returned error.
    ///     Description of the error is then in the properties LastError and LastHttpStatusCode.
    /// </summary>
    public partial class ProjectApi : BaseAuthApi<ProjectApi>
    {
        public async Task<Project> UploadMediaFileAsync(int projectId, long fileSize, string fileName, Stream fileContent,
            CancellationToken cancellationToken)
        {
            var builder = CreateBuilder()
                .AddUrlSegment(projectId.ToString())
                .AddUrlSegment("Files")
                .AddUrlSegment("UploadMediaFile")
                .AddParameter("fileSize", fileSize.ToString())
                .AddFile(fileName, fileContent);

            var result = await builder.ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<Project>(r.GetStringContent()));
        }
        public async Task<Project> UploadMediaFileAsync(int projectId, long fileSize, string fileName, byte[] fileContent,
            CancellationToken cancellationToken)
        {
            MemoryStream memoryStream = CreateMemoryStream(fileContent);

            try { return await UploadMediaFileAsync(projectId, fileSize, fileName, memoryStream, cancellationToken); }
            finally { memoryStream.Close(); }
        }
        public async Task<Project> UploadMediaFileAsync(int projectId, long fileSize, System.IO.FileInfo fileInfo,
            CancellationToken cancellationToken)
        {
            FileStream fileStream = CreateFileStream(fileInfo);

            try { return await UploadMediaFileAsync(projectId, fileSize, fileInfo.Name, fileStream, cancellationToken); }
            finally { fileStream.Close(); }
        }

        public async Task<Stream> DownloadAudioInitAsync(int projectId,
            CancellationToken cancellationToken)
        {
            var builder = CreateBuilder()
                .AddUrlSegment(projectId.ToString())
                .AddUrlSegment("Files/Audio/Init");

            var result = await builder.ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => r.Content);
        }
        public async Task<Stream> DownloadVideoInitAsync(int projectId,
            CancellationToken cancellationToken)
        {
            var builder = CreateBuilder()
                .AddUrlSegment(projectId.ToString())
                .AddUrlSegment("Files/Video/Init");

            var result = await builder.ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => r.Content);
        }
        public async Task<Stream> DownloadAudioSegmentAsync(int projectId, int segment,
            CancellationToken cancellationToken)
        {
            var builder = CreateBuilder()
                .AddUrlSegment(projectId.ToString())
                .AddUrlSegment("Files/Audio/Segment")
                .AddUrlSegment(segment.ToString());

            var result = await builder.ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => r.Content);
        }
        public async Task<Stream> DownloadVideoSegmentAsync(int projectId, int segment,
            CancellationToken cancellationToken)
        {
            var builder = CreateBuilder()
                .AddUrlSegment(projectId.ToString())
                .AddUrlSegment("Files/Video/Segment")
                .AddUrlSegment(segment.ToString());

            var result = await builder.ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => r.Content);
        }
        public async Task<Stream> DownloadMediaFileAsync(int projectId,
            CancellationToken cancellationToken)
        {
            var builder = CreateBuilder()
                .AddUrlSegment(projectId.ToString())
                .AddUrlSegment("Files/MediaFile");

            var result = await builder.ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => r.Content);
        }
        public async Task<Stream> DownloadMpdManifestAsync(int projectId,
            CancellationToken cancellationToken)
        {
            var builder = CreateBuilder()
                .AddUrlSegment(projectId.ToString())
                .AddUrlSegment("Files/MpdManifest");

            var result = await builder.ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => r.Content);
        }


        private FileStream CreateFileStream(FileInfo fileInfo)
        {
            FileStream fileStream;
            try { fileStream = fileInfo.OpenRead(); }
            catch (Exception ex)
            {
                Utility.LogApiException(ex, Logger);
                throw;
            }

            return fileStream;
        }
        private MemoryStream CreateMemoryStream(byte[] fileContent)
        {
            MemoryStream memoryStream;
            try { memoryStream = new MemoryStream(fileContent); }
            catch (Exception ex)
            {
                Utility.LogApiException(ex, Logger);
                throw;
            }

            return memoryStream;
        }
    }
}
