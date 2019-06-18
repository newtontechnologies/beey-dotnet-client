using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Auth;
using Beey.DataExchangeModel.Projects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
    public class FilesApi : BaseAuthApi<FilesApi>
    {
        public FilesApi(string url) : base(url)
        {
            EndPoint = "API/Files/";
        }

        public async Task<System.IO.Stream> DownloadTrsxAsync(int projectId, int trsxId,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
               .AddUrlSegment("Download")
               .AddParameter("projectId", projectId.ToString())
               .AddParameter("fileId", trsxId.ToString())
               .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, _ => result.Content);
        }

        public async Task UploadFileAsync(int projectId, string fileName, System.IO.Stream fileContent,
            string language, bool transcribe,
            CancellationToken cancellationToken)
        {
            var builder = CreateBuilder()
               .AddUrlSegment("Recognize")
               .AddParameter("projectId", projectId.ToString())
               .AddParameter("lang", language)
               .AddParameter("transcribe", transcribe.ToString().ToLower());

            builder.AddFile(fileName, fileContent);

            var result = await builder.ExecuteAsync(HttpMethod.POST, cancellationToken);

            HandleResponse(result, _ => true);
        }
        public async Task UploadFileAsync(int projectId, string fileName, byte[] fileContent,
            string language, bool transcribe,
            CancellationToken cancellationToken)
        {
            System.IO.MemoryStream memoryStream;
            try { memoryStream = new System.IO.MemoryStream(fileContent); }
            catch (Exception ex)
            {
                Utility.LogApiException(ex, Logger);
                throw;
            }

            try { await UploadFileAsync(projectId, fileName, memoryStream, language, transcribe, cancellationToken); }
            catch (Exception) { throw; }
            finally { memoryStream.Close(); }
        }
        public async Task UploadFileAsync(int projectId, System.IO.FileInfo fileInfo,
            string language, bool transcribe,
            CancellationToken cancellationToken)
        {
            System.IO.FileStream fileStream;
            try { fileStream = fileInfo.OpenRead(); }
            catch (Exception ex)
            {
                Utility.LogApiException(ex, Logger);
                throw;
            }

            try { await UploadFileAsync(projectId, fileInfo.Name, fileStream, language, transcribe, cancellationToken); }
            catch (Exception) { throw; }
            finally { fileStream.Close(); }
        }

        public async Task<System.IO.Stream> DownloadFileAsync(int projectId, int recordingId,
            CancellationToken cancellationToken)
        {
            var builder = CreateBuilder()
               .AddUrlSegment("Download")
               .AddParameter("projectId", projectId.ToString())
               .AddParameter("fileId", recordingId.ToString());

            var result = await builder.ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => r.Content);
        }
    }
}
