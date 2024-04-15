using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Beey.DataExchangeModel.Auth;

namespace Beey.Api.Rest;

/// <summary>
/// All methods:
/// 1) returns result if everything is ok,
/// 2) throws exception when there is some network or framework problem,
/// 3a) returns null if everything is ok, but backend returned error.
/// 3b) throws exception when server returned 500
///     Description of the error is then in the properties LastError and LastHttpStatusCode..
/// </summary>
public class LoginApi : BaseApi<LoginApi>
{
    public LoginApi(string url) : base(url)
    {
        EndPoint = "XAPI";
    }

    public async Task<LoginToken> LoginAsync(string email, string password,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("Login")
            .SetBody(JsonSerializer.Serialize(new LoginDto { Email = email, Password = password }), "application/json")
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<LoginToken>(r.GetStringContent()));
    }

    public async Task LogoutAsync(LoginToken token, CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("Logout")
            .AddHeader("Authorization", token.Token)
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        HandleResponse(result);
    }

    public async Task<LoginToken> RegisterAndLoginAsync(string email, string password, string language,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("RegisterAndLogin")
            .SetBody(JsonSerializer.Serialize(new RegistrationDto { Email = email, Password = password, Language = language }), "application/json")
            .AddParameters(("email", email), ("password", password))
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        return await HandleResponseAsync(result, async (r, c) => await LoginAsync(email, password, c),
            cancellationToken);
    }

    public async Task<string> GetContentVersionAsync(CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("ContentVersion")
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => ((JsonObject)JsonNode.Parse(r.GetStringContent())).TryGetPropertyValue("Main", out var value) && value.GetValue<string>() is { } v ? v : null);
    }

    public async Task<JsonObject> GetPasswordSettingsAsync(CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("PasswordSettings")
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => (JsonObject)JsonNode.Parse(r.GetStringContent()));
    }
}
