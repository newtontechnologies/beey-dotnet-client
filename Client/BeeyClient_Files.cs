using System.Threading;
using System.Threading.Tasks;
using Beey.Api.Rest;

namespace Beey.Client
{
    public partial class BeeyClient
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

        public async Task UploadFileAsync(int projectId, long accessToken, string fileName, byte[] fileContent,
            string language = "cz", bool transcribe = true,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            await policy.ExecuteAsync(async (c) =>
            {
                await FilesApi.UploadFileAsync(projectId, accessToken, fileName, fileContent, language, transcribe, cancellationToken);
                return true;
            }, cancellationToken);
        }

        public async Task UploadFileAsync(int projectId, long accessToken, string fileName, System.IO.Stream fileContent,
            string language = "cz", bool transcribe = true,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            await policy.ExecuteAsync(async (c) =>
            {
                await FilesApi.UploadFileAsync(projectId, accessToken, fileName, fileContent, language, transcribe, cancellationToken);
                return true;
            }, cancellationToken);
        }

        public async Task UploadFileAsync(int projectId,long accessToken, System.IO.FileInfo fileInfo,
            string language = "cz", bool transcribe = true,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            await policy.ExecuteAsync(async (c) =>
            {
                await FilesApi.UploadFileAsync(projectId, accessToken, fileInfo, language, transcribe, cancellationToken);
                return true;
            }, cancellationToken);
        }

        public async Task<System.IO.Stream> DownloadFileAsync(int projectId, int recordingId,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<System.IO.Stream>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await FilesApi.DownloadFileAsync(projectId, recordingId, cancellationToken);
            }, cancellationToken));
        }
    }
}
