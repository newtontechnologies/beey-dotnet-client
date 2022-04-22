using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Beey.Api.Rest;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Beey.Client;

public partial class BeeyClient
{
    public async Task<IEnumerable<string>> GetTagsAsync(int id,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<string>();
        var tags = (await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await ProjectApi.GetTagsAsync(id, c);
        }, CreatePollyContext(cancellationToken), cancellationToken));

        var jTags = JArray.Parse(tags);
        return jTags.Select(t => t.Value<string>());
    }
    public async Task<Project> AddTagAsync(int id, long accessToken, string tag,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
        return (await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await ProjectApi.AddTagAsync(id, accessToken, tag, c);
        }, CreatePollyContext(cancellationToken), cancellationToken));
    }
    public async Task<Project> DeleteTagAsync(int id, long accessToken, string tag,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
        return (await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await ProjectApi.RemoveTagAsync(id, accessToken, tag, c);
        }, CreatePollyContext(cancellationToken), cancellationToken));
    }

    public async Task<ProjectMetadata> GetMetadataAsync(int id, string key,
       CancellationToken cancellationToken)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<ProjectMetadata>();
        return await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await ProjectApi.GetMetadataAsync(id, key, c);
        }, CreatePollyContext(cancellationToken), cancellationToken);
    }
    public async Task<Project> AddMetadataAsync(int id, long accessToken,
        string key, string value,
       CancellationToken cancellationToken)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
        return (await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await ProjectApi.AddMetadataAsync(id, accessToken, key, value, c);
        }, CreatePollyContext(cancellationToken), cancellationToken));
    }
    public async Task<Project> RemoveMetadataAsync(int id, long accessToken, string key,
       CancellationToken cancellationToken)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
        return (await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await ProjectApi.RemoveMetadataAsync(id, accessToken, key, c);
        }, CreatePollyContext(cancellationToken), cancellationToken));
    }
}
