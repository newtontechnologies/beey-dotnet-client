using System.Threading;
using System.Threading.Tasks;
using Beey.Api.Rest;
using Beey.DataExchangeModel.Projects;

namespace Beey.Client
{
    public partial class BeeyClient
    {
        public async Task<System.IO.Stream> DownloadCurrentTrsxAsync(int projectId,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<System.IO.Stream>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.DownloadCurrentTrsxAsync(projectId, c);
            }, cancellationToken));
        }

        public async Task<System.IO.Stream> DownloadOriginalTrsxAsync(int projectId,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<System.IO.Stream>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.DownloadOriginalTrsxAsync(projectId, c);
            }, cancellationToken));
        }

        public async Task<Project> UploadMediaFileAsync(int projectId, long fileSize, string fileName, byte[] fileContent,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.UploadMediaFileAsync(projectId, fileSize, fileName, fileContent, cancellationToken);
            }, cancellationToken);
        }

        public async Task<Project> UploadMediaFileAsync(int projectId, long fileSize, string fileName, System.IO.Stream fileContent,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.UploadMediaFileAsync(projectId, fileSize, fileName, fileContent, cancellationToken);
            }, cancellationToken);
        }

        public async Task<Project> UploadMediaFileAsync(int projectId, long fileSize, System.IO.FileInfo fileInfo,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.UploadMediaFileAsync(projectId, fileSize, fileInfo, cancellationToken);
            }, cancellationToken);
        }

        public async Task<Project> TranscribeProjectAsync(int projectId,
            string language = "cz", bool withPpc = true, bool saveTrsx = true,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.TranscribeProjectAsync(projectId, language, withPpc, saveTrsx, cancellationToken);
            }, cancellationToken);
        }

        public async Task<System.IO.Stream> DownloadAudioAsync(int projectId,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<System.IO.Stream>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.DownloadAudioAsync(projectId, cancellationToken);
            }, cancellationToken));
        }
        public async Task<System.IO.Stream> DownloadVideoAsync(int projectId,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<System.IO.Stream>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.DownloadVideoAsync(projectId, cancellationToken);
            }, cancellationToken));
        }
        public async Task<System.IO.Stream> DownloadMpdManifestAsync(int projectId,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<System.IO.Stream>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.DownloadMpdManifestAsync(projectId, cancellationToken);
            }, cancellationToken));
        }
    }
}
