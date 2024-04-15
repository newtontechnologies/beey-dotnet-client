using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Projects;

namespace Beey.Api.Rest;

public partial class ProjectApi : BaseAuthApi<ProjectApi>
{
    public async Task<ProjectDto> ShareProjectAsync(int id, long accessToken, string email,
       CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(id.ToString())
            .AddUrlSegment("Share")
            .AddParameter("shareTo", email)
            .AddParameter("accessToken", accessToken)
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<ProjectDto>(r.GetStringContent()));
    }

    public async Task<Listing<ProjectAccessDto>> ListProjectSharing(int id,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(id.ToString())
            .AddUrlSegment("Share/List")
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<Listing<ProjectAccessDto>>(r.GetStringContent()));
    }
}
