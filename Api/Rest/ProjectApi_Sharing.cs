using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Projects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api.Rest
{
    public partial class ProjectApi : BaseAuthApi<ProjectApi>
    {
        public async Task<Project> ShareProjectAsync(int id, long accessToken, string email,
           CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment(id.ToString())
                .AddUrlSegment("Share")
                .AddParameter("shareTo", email)
                .AddParameter("accessToken", accessToken)
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<Project>(r.GetStringContent()));
        }

        public async Task<Listing<ProjectAccess>> ListProjectSharing(int id,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment(id.ToString())
                .AddUrlSegment("Share/List")
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<Listing<ProjectAccess>>(r.GetStringContent()));
        }
    }
}
