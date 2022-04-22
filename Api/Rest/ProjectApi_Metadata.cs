using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Auth;
using Beey.DataExchangeModel.Projects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api.Rest;

/// <summary>
/// All methods:
/// 1) returns result if everything is ok,
/// 2) throws exception when there is some network or framework problem,
/// 3a) throws exception when server returned 500
/// 3b) returns null if everything is ok, but backend returned error.
///     Description of the error is then in the properties LastError and LastHttpStatusCode.
/// </summary>
public partial class ProjectApi : BaseAuthApi<ProjectApi>
{
    public async Task<string> GetTagsAsync(int id,
       CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(id.ToString())
            .AddUrlSegment("Metadata/Tags")
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => r.GetStringContent());
    }

    public async Task<Project> AddTagAsync(int id, long accessToken, string tag,
       CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(id.ToString())
            .AddUrlSegment("Metadata/Tags")
            .AddParameter("accessToken", accessToken)
            .AddParameter("tag", tag)
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        return HandleResponse(result, r => JsonConvert.DeserializeObject<Project>(r.GetStringContent()));
    }

    public async Task<Project> RemoveTagAsync(int id, long accessToken, string tag,
       CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(id.ToString())
            .AddUrlSegment("Metadata/Tags")
            .AddParameter("accessToken", accessToken)
            .AddParameter("tag", tag)
            .ExecuteAsync(HttpMethod.DELETE, cancellationToken);

        return HandleResponse(result, r => JsonConvert.DeserializeObject<Project>(r.GetStringContent()));
    }

    public async Task<ProjectMetadata> GetMetadataAsync(int id, string key,
       CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(id.ToString())
            .AddUrlSegment("Metadata")
            .AddParameter("key", key)
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonConvert.DeserializeObject<ProjectMetadata>(r.GetStringContent()));
    }

    public async Task<Project> AddMetadataAsync(int id, long accessToken,
        string key, string value,
       CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(id.ToString())
            .AddUrlSegment("Metadata")
            .AddParameter("accessToken", accessToken)
            .AddParameter("key", key)
            .SetBody(value)
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        return HandleResponse(result, r => JsonConvert.DeserializeObject<Project>(r.GetStringContent()));
    }

    public async Task<Project> RemoveMetadataAsync(int id, long accessToken, string key,
       CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(id.ToString())
            .AddUrlSegment("Metadata")
            .AddParameter("accessToken", accessToken)
            .AddParameter("key", key)
            .ExecuteAsync(HttpMethod.DELETE, cancellationToken);

        return HandleResponse(result, r => JsonConvert.DeserializeObject<Project>(r.GetStringContent()));
    }
}
