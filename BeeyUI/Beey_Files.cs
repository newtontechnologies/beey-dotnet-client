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
        public async Task<System.IO.Stream?> DownloadTrsxAsync(int projectId, int trsxId,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<System.IO.Stream?>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await FilesApi.DownloadTrsxAsync(projectId, trsxId, c);
            }, cancellationToken));
        }

        public async Task<bool> UploadFileAsync(int projectId, string fileName, byte[] fileContent,
            string language = "cz", bool transcribe = true,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await FilesApi.UploadFileAsync(projectId, fileName, fileContent, language, transcribe, cancellationToken);
            }, cancellationToken));
        }

        public async Task<bool> UploadFileAsync(int projectId, string fileName, System.IO.Stream fileContent,
            string language = "cz", bool transcribe = true,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await FilesApi.UploadFileAsync(projectId, fileName, fileContent, language, transcribe, cancellationToken);
            }, cancellationToken));
        }

        public async Task<bool> UploadFileAsync(int projectId, System.IO.FileInfo fileInfo,
            string language = "cz", bool transcribe = true,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await FilesApi.UploadFileAsync(projectId, fileInfo, language, transcribe, cancellationToken);
            }, cancellationToken));
        }
    }
}
