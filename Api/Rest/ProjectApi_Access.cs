using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Beey.DataExchangeModel.Projects;

namespace Beey.Api.Rest;

public partial class ProjectApi : BaseAuthApi<ProjectApi>
{
    public async Task<ProjectAccessDto> GetProjectAccessAsync(int projectId,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(projectId.ToString())
            .AddUrlSegment("Access")
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<ProjectAccessDto>(r.GetStringContent()));
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
