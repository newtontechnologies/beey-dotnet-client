using BeeyApi.POCO;
using BeeyApi.POCO.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeeyApi.Rest;
using System.Net;

namespace BeeyUI
{
    public partial class Beey
    {
        public async Task<System.IO.Stream?> DownloadTrsxAsync(int projectId, int trsxId, CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            return (await CreateHttpAsyncUnauthorizedPolicy(() => default(System.IO.Stream)).ExecuteAsync(async (c) =>
            {
                return await FilesApi.DownloadTrsxAsync(projectId, trsxId, c);
            }, cancellationToken));
        }

        public async Task<bool> UploadFileAsync(string fileName, byte[] fileContent, int projectId, string language, bool transcribe = true, CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy(() => false);
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await FilesApi.UploadFileAsync(fileName, fileContent, projectId, language, transcribe, cancellationToken);
            }, cancellationToken));
        }

        public async Task<bool> UploadFileAsync(string fileName, System.IO.Stream fileContent, int projectId, string language, bool transcribe = true, CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy(() => false);
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await FilesApi.UploadFileAsync(fileName, fileContent, projectId, language, transcribe, cancellationToken);
            }, cancellationToken));
        }
    }
}
