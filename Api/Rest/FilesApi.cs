﻿using Beey.DataExchangeModel;
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

        public async Task<Project> UploadFileAsync(int projectId, long accessToken, string fileName, System.IO.Stream fileContent,
            string language, bool transcribe,
            CancellationToken cancellationToken)
        {
            var builder = CreateBuilder()
               .AddUrlSegment("Recognize")
               .AddParameter("projectId", projectId.ToString())
               .AddParameter("accessToken", accessToken.ToString())
               .AddParameter("lang", language)
               .AddParameter("transcribe", transcribe.ToString().ToLower());

            builder.AddFile(fileName, fileContent);

            var result = await builder.ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<Project>(r.GetStringContent()));
        }
        public async Task<Project> UploadFileAsync(int projectId, long accessToken, string fileName, byte[] fileContent,
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

            try { return await UploadFileAsync(projectId, accessToken, fileName, memoryStream, language, transcribe, cancellationToken); }
            catch (Exception) { throw; }
            finally { memoryStream.Close(); }
        }
        public async Task<Project> UploadFileAsync(int projectId, long accessToken, System.IO.FileInfo fileInfo,
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

            try { return await UploadFileAsync(projectId, accessToken, fileInfo.Name, fileStream, language, transcribe, cancellationToken); }
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
