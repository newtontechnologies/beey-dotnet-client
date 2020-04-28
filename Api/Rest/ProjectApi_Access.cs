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
        public async Task<ProjectAccess> GetProjectAccessAsync(int projectId,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment(projectId.ToString())
                .AddUrlSegment("Access")
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<ProjectAccess>(r.GetStringContent()));
        }

        public async Task UpdateProjectAccessAsync(int projectId, ProjectAccess projectAccess,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment(projectId.ToString())
                .AddUrlSegment("Access")
                .SetBody(JsonConvert.SerializeObject(projectAccess), "application/json")
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            HandleResponse(result);
        }
    }
}
