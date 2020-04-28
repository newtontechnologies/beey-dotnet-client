using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Beey.Api.Rest;
using Beey.DataExchangeModel.Projects;

namespace Beey.Client
{
    public partial class BeeyClient
    {
        public async Task<Stream> DownloadCurrentTrsxAsync(int projectId,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Stream>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.DownloadCurrentTrsxAsync(projectId, c);
            }, cancellationToken));
        }

        public async Task<Stream> DownloadOriginalTrsxAsync(int projectId,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Stream>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.DownloadOriginalTrsxAsync(projectId, c);
            }, cancellationToken));
        }

        public async Task<Project> UploadCurrentTrsxAsync(int projectId, long accessToken, string fileName, byte[] trsx,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.UploadCurrentTrsxAsync(projectId, accessToken, fileName, trsx, c);
            }, cancellationToken);
        }
        public async Task<Project> UploadCurrentTrsxAsync(int projectId, long accessToken, string fileName, Stream trsx,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.UploadCurrentTrsxAsync(projectId, accessToken, fileName, trsx, c);
            }, cancellationToken);
        }

        public async Task<Project> UploadOriginalTrsxAsync(int projectId, long accessToken, string fileName, byte[] trsx,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.UploadOriginalTrsxAsync(projectId, accessToken, fileName, trsx, c);
            }, cancellationToken);
        }
        public async Task<Project> UploadOriginalTrsxAsync(int projectId, long accessToken, string fileName, Stream trsx,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.UploadOriginalTrsxAsync(projectId, accessToken, fileName, trsx, c);
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

        public async Task<Stream> DownloadAudioAsync(int projectId,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Stream>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.DownloadAudioAsync(projectId, cancellationToken);
            }, cancellationToken));
        }
        public async Task<Stream> DownloadVideoAsync(int projectId,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Stream>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.DownloadVideoAsync(projectId, cancellationToken);
            }, cancellationToken));
        }
        public async Task<Stream> DownloadMpdManifestAsync(int projectId,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Stream>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.DownloadMpdManifestAsync(projectId, cancellationToken);
            }, cancellationToken));
        }
        public async Task<Stream> DownloadAudioSegmentAsync(int projectId, int segment,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Stream>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.DownloadAudioSegmentAsync(projectId, segment, cancellationToken);
            }, cancellationToken));
        }
        public async Task<Stream> DownloadVideoSegmentAsync(int projectId, int segment,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Stream>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.DownloadVideoSegmentAsync(projectId, segment, cancellationToken);
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

        public async Task<Project> UploadMediaFileAsync(int projectId, long fileSize, string fileName, Stream fileContent,
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
    }
}
