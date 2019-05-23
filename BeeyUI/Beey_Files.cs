using BeeyApi.POCO;
using BeeyApi.POCO.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeeyApi.Rest;

namespace BeeyUI
{
    public partial class Beey
    {
        public async Task<System.IO.Stream?> DownloadTrsxAsync(int projectId, int trsxId, CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            return (await CreateHttpAsyncUnauthorizedPolicy(() => default(System.IO.Stream)).ExecuteAsync(async (c) =>
            {
                var result = await FilesApi.DownloadTrsxAsync(projectId, trsxId, c);
                return (result, FilesApi.LastHttpStatusCode);
            }, cancellationToken)).Result;
        }

        public async Task<bool> UploadFileAsync(IEnumerable<(string Name, byte[] Content)> files, int projectId, string language, bool transcribe = true, CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();
            var policy = CreateHttpAsyncUnauthorizedPolicy(() => false);
            return (await policy.ExecuteAsync(async (c) =>
            {
                var result = await FilesApi.UploadFileAsync(files, projectId, language, transcribe, cancellationToken);
                return (result, FilesApi.LastHttpStatusCode);
            }, cancellationToken)).Result;
        }

        public async Task<bool> UploadFileAsync(IEnumerable<(string Name, System.IO.Stream Content)> files, int projectId, string language, bool transcribe = true, CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy(() => false);
            return (await policy.ExecuteAsync(async (c) =>
            {
                var result = await FilesApi.UploadFileAsync(files, projectId, language, transcribe, cancellationToken);
                return (result, FilesApi.LastHttpStatusCode);
            }, cancellationToken)).Result;
        }
    }
}
