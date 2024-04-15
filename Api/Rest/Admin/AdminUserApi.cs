using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Beey.Api.DTO;
using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Auth;

namespace Beey.Api.Rest.Admin;

public class AdminUserApi : BaseAuthApi<AdminUserApi>
{
    public AdminUserApi(string url) : base(url)
    {
        EndPoint = "API/Admin/User";
    }

    public async Task<Listing<UserDto>> ListAsync(int count, int skip, CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
        .AddUrlSegment("List")
        .AddParameter("skip", skip)
        .AddParameter("count", count)
        .ExecuteAsync(HttpMethod.POST, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<Listing<UserDto>>(r.GetStringContent()));
    }

    public async Task<UserDto> GetAsync(int id, CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(id.ToString())
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<UserDto>(r.GetStringContent()));
    }

    public async Task<UserDto> CreateAsync(UserAddDto user,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .SetBody(JsonSerializer.Serialize(user), "application/json")
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<UserDto>(r.GetStringContent()));
    }

    public async Task UpdateAsync(UserUpdateDto user,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .SetBody(JsonSerializer.Serialize(user), "application/json")
            .ExecuteAsync(HttpMethod.PUT, cancellationToken);

        HandleResponse(result);
    }

    public async Task<bool> DeleteAsync(int id,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(id.ToString())
            .ExecuteAsync(HttpMethod.DELETE, cancellationToken);

        if (ResultNotFound(result))
        {
            return false;
        }

        return HandleResponse(result, _ => true);
    }

    public async Task<Listing<MonthlyTranscriptionLogItem>> GetTranscriptionLogAsync(int id,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(id.ToString())
            .AddUrlSegment("TranscriptionLog")
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<Listing<MonthlyTranscriptionLogItem>>(r.GetStringContent()));
    }
}
