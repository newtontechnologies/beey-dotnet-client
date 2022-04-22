using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Beey.Api.Rest;
using Beey.DataExchangeModel.Projects;

namespace Beey.Client;

public partial class BeeyClient
{
    public async Task<Stream> DownloadAudioInitAsync(int projectId,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<Stream>();
        return (await policy.ExecuteAsync(async (c) =>
        {
            return await ProjectApi.DownloadAudioInitAsync(projectId, cancellationToken);
        }, cancellationToken));
    }
    public async Task<Stream> DownloadVideoInitAsync(int projectId,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<Stream>();
        return (await policy.ExecuteAsync(async (c) =>
        {
            return await ProjectApi.DownloadVideoInitAsync(projectId, cancellationToken);
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

    public async Task<Stream> DownloadMediaFileAsync(int projectId,
       CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<Stream>();
        return (await policy.ExecuteAsync(async (c) =>
        {
            return await ProjectApi.DownloadMediaFileAsync(projectId, cancellationToken);
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
