using Beey.DataExchangeModel.Projects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api.Rest;

public partial class ProjectApi : BaseAuthApi<ProjectApi>
{
    public async Task<ProjectAccessViewModel> GetProjectAccessAsync(int projectId,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(projectId.ToString())
            .AddUrlSegment("Access")
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<ProjectAccessViewModel>(r.GetStringContent()));
    }

    public async Task UpdateProjectAccessAsync(int projectId, ProjectAccessUpdateModel projectAccess,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(projectId.ToString())
            .AddUrlSegment("Access")
            .SetBody(JsonSerializer.Serialize(projectAccess), "application/json")
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        HandleResponse(result);
    }
}
