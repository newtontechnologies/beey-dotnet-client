using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Auth;
using Beey.DataExchangeModel.Lexicons;
using Beey.DataExchangeModel.Messaging;
using Beey.DataExchangeModel.Projects;
using Beey.DataExchangeModel.Users;

namespace Beey.Api.Rest;

public class CurrentUserApi : BaseAuthApi<CurrentUserApi>
{
    public CurrentUserApi(string url) : base(url)
    {
        EndPoint = "XAPI/CurrentUser";
    }

    public async Task ChangePasswordAsync(string oldPassword, string newPassword,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("ChangePassword")
            .AddParameters(("password", oldPassword), ("newPassword", newPassword))
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        HandleResponse(result);
    }

    public async Task<LoginToken> GetUserInfoAsync(CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<LoginToken>(r.GetStringContent()));
    }


    public async Task<JsonObject> GetUserSettingsAsync(CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("Settings")
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => (JsonObject)JsonNode.Parse(r.GetStringContent()));
    }

    public async Task PostUserSettingsAsync(JsonObject settings,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("Settings")
            .SetBody(settings.ToString())
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        HandleResponse(result);
    }

    public async Task<AggregatedListing<TranscriptionLogItemDto, decimal>> GetTranscriptionLogAsync(CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("TranscriptionLog")
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<AggregatedListing<TranscriptionLogItemDto, decimal>>(r.GetStringContent()));
    }

    public async Task<LexiconEntryDto[]> GetUserLexAsync(string language, CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("Lexicon")
            .AddParameter("language", language)
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<LexiconEntryDto[]>(r.GetStringContent(), new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    public async Task SetUserLexAsync(string language, IEnumerable<LexiconEntryDto> userLex,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("Lexicon")
            .AddParameter("language", language)
            .SetBody(JsonSerializer.Serialize(userLex, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }), "application/json")
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        HandleResponse(result);
    }

    public async Task<List<string>> ListUserLexLanguagesAsync(CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("Lexicon")
            .AddUrlSegment("ListLanguages")
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<List<string>>(r.GetStringContent()));
    }

    public async Task<Message[]> GetUserMessagesAsync(DateTime? from, CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("MessageCache")
            .AddParameter("from", from)
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<Message[]>(r.GetStringContent(), GetJsonSerializerOptions()));
    }

    public async Task<PaymentInfoDto> GetPaymentInfoAsync(CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .EndPoint("XAPI/User")
            .AddUrlSegment("PaymentInfo")
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<PaymentInfoDto>(r.GetStringContent(), GetJsonSerializerOptions()));
    }

    public async Task<PaymentInfoDto> SetPaymentInfoAsync(PaymentInfoDto paymentInfo, CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .EndPoint("XAPI/User")
            .AddUrlSegment("PaymentInfo")
            .SetBody(JsonSerializer.Serialize(paymentInfo))
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<PaymentInfoDto>(r.GetStringContent(), GetJsonSerializerOptions()));
    }

    public async Task<bool> GetDataProtectionConsentAsync(CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .EndPoint("XAPI/User")
            .AddUrlSegment("DataProtectionConsent")
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<bool>(r.GetStringContent(), GetJsonSerializerOptions()));
    }

    public async Task<UserDto> SetDataProtectionConsentAsync(bool consent, CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .EndPoint("XAPI/User")
            .AddUrlSegment("DataProtectionConsent")
            .AddParameter("consent", consent)
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<UserDto>(r.GetStringContent(), GetJsonSerializerOptions()));
    }

    private static JsonSerializerOptions GetJsonSerializerOptions() => Message.DefaultJsonSerializerOptions;
}
