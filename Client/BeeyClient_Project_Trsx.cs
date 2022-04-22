using Beey.DataExchangeModel.Projects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Client;

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
}
