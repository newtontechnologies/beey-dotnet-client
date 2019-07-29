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

namespace Beey.Api.Rest
{
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
                .AddUrlSegment("Metadata/Tags")
                .AddParameter("id", id)
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => r.GetStringContent());
        }

        public async Task<Project> AddTagAsync(int id, long accessToken, string tag,
           CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("Metadata/Tag")
                .AddParameter("id", id)
                .AddParameter("accessToken", accessToken)
                .AddParameter("tag", tag)
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<Project>(r.GetStringContent()));
        }

        public async Task<Project> RemoveTagAsync(int id, long accessToken, string tag,
           CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("Metadata/Tag")
                .AddParameter("id", id)
                .AddParameter("accessToken", accessToken)
                .AddParameter("tag", tag)
                .ExecuteAsync(HttpMethod.DELETE, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<Project>(r.GetStringContent()));
        }
    }
}
