using System.Threading;
using System.Threading.Tasks;
using Beey.Api.Rest;
using Beey.DataExchangeModel.Projects;

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

        public async Task<Project> UploadFileAsync(int projectId, long accessToken, string fileName, byte[] fileContent,
            string language = "cz", bool transcribe = true,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await FilesApi.UploadFileAsync(projectId, accessToken, fileName, fileContent, language, transcribe, cancellationToken);
            }, cancellationToken);
        }

        public async Task<Project> UploadFileAsync(int projectId, long accessToken, string fileName, System.IO.Stream fileContent,
            string language = "cz", bool transcribe = true,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await FilesApi.UploadFileAsync(projectId, accessToken, fileName, fileContent, language, transcribe, cancellationToken);
            }, cancellationToken);
        }

        public async Task<Project> UploadFileAsync(int projectId,long accessToken, System.IO.FileInfo fileInfo,
            string language = "cz", bool transcribe = true,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await FilesApi.UploadFileAsync(projectId, accessToken, fileInfo, language, transcribe, cancellationToken);
            }, cancellationToken);
        }

        public async Task<Project> TranscribeProjectAsync(int projectId, long accessToken, string language = "cz",
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await FilesApi.TranscribeProjectAsync(projectId, accessToken, language, cancellationToken);
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
