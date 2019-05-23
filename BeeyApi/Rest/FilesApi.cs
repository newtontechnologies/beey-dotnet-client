using BeeyApi.POCO;
using BeeyApi.POCO.Auth;
using BeeyApi.POCO.Projects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeeyApi.Rest
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

        public async Task<System.IO.Stream?> DownloadTrsxAsync(int projectId, int trsxId, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
               .AddUrlSegment("Download")
               .AddParameter("projectId", projectId.ToString())
               .AddParameter("fileId", trsxId.ToString())
               .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse<System.IO.Stream?>(result, HttpStatusCode.OK,
                _ => result.Content,
                _ => default);
        }

        public async Task<bool> UploadFileAsync(IEnumerable<(string Name, byte[] Content)> files, int projectId, string language, bool transcribe, CancellationToken cancellationToken)
        {
            var builder = CreateBuilder()
               .AddUrlSegment("Recognize")
               .AddParameter("projectId", projectId.ToString())
               .AddParameter("lang", language)
               .AddParameter("transcribe", transcribe.ToString().ToLower());

            foreach (var (Name, Content) in files)
            {
                builder.AddFile(Name, Content);
            }

            var result = await builder.ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, HttpStatusCode.OK,
                _ => true,
                _ => false);
        }

        public async Task<bool> UploadFileAsync(IEnumerable<(string Name, System.IO.Stream Content)> files, int projectId, string language, bool transcribe, CancellationToken cancellationToken)
        {
            var builder = CreateBuilder()
               .AddUrlSegment("Recognize")
               .AddParameter("projectId", projectId.ToString())
               .AddParameter("lang", language)
               .AddParameter("transcribe", transcribe.ToString());

            foreach (var (Name, Content) in files)
            {
                builder.AddFile(Name, Content);
            }

            var result = await builder.ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, HttpStatusCode.OK,
                _ => true,
                _ => false);
        }
    }
}
