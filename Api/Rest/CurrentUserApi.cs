using Beey.Api.DTO;
using Beey.DataExchangeModel.Auth;
using Beey.DataExchangeModel.Lexicons;
using Beey.DataExchangeModel.Messaging;
using Beey.DataExchangeModel.Projects;
using Beey.DataExchangeModel.Serialization.JsonConverters;
using Beey.DataExchangeModel.Users;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

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

        return HandleResponse(result, r => JsonConvert.DeserializeObject<LoginToken>(r.GetStringContent()));
    }


    public async Task<JObject> GetUserSettingsAsync(CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("Settings")
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JObject.Parse(r.GetStringContent()));
    }

    public async Task PostUserSettingsAsync(JObject settings,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("Settings")
            .SetBody(settings.ToString())
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        HandleResponse(result);
    }

    public async Task<MonthlyTranscriptionLogItem[]> GetTranscriptionLogAsync(CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("TranscriptionLog")
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonConvert.DeserializeObject<MonthlyTranscriptionLogItem[]>(r.GetStringContent()));
    }

    public async Task<LexiconEntry[]> GetUserLexAsync(string language, CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("Lexicon")
            .AddParameter("language", language)
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonConvert.DeserializeObject<LexiconEntry[]>(r.GetStringContent()));
    }

    public async Task SetUserLexAsync(string language, IEnumerable<LexiconEntry> userLex,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("Lexicon")
            .AddParameter("language", language)
            .SetBody(JsonConvert.SerializeObject(userLex), "application/json")
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        HandleResponse(result);
    }

    public async Task<List<string>> ListUserLexLanguagesAsync(CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("Lexicon")
            .AddUrlSegment("ListLanguages")
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonConvert.DeserializeObject<List<string>>(r.GetStringContent()));
    }

    public async Task<Message[]> GetUserMessagesAsync(DateTime? from, CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("MessageCache")
            .AddParameter("from", from)
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => System.Text.Json.JsonSerializer.Deserialize<Message[]>(r.GetStringContent(), GetJsonSerializerOptions()));
    }

    public async Task<PaymentInfoViewModel> GetPaymentInfoAsync(CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .EndPoint("XAPI/User")
            .AddUrlSegment("PaymentInfo")
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => System.Text.Json.JsonSerializer.Deserialize<PaymentInfoViewModel>(r.GetStringContent(), GetJsonSerializerOptions()));
    }

    public async Task<PaymentInfoViewModel> SetPaymentInfoAsync(PaymentInfoViewModel paymentInfo, CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .EndPoint("XAPI/User")
            .AddUrlSegment("PaymentInfo")
            .SetBody(System.Text.Json.JsonSerializer.Serialize(paymentInfo))
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        return HandleResponse(result, r => System.Text.Json.JsonSerializer.Deserialize<PaymentInfoViewModel>(r.GetStringContent(), GetJsonSerializerOptions()));
    }

    public async Task<bool> GetDataProtectionConsentAsync(CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .EndPoint("XAPI/User")
            .AddUrlSegment("DataProtectionConsent")
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => System.Text.Json.JsonSerializer.Deserialize<bool>(r.GetStringContent(), GetJsonSerializerOptions()));
    }

    public async Task<UserViewModel> SetDataProtectionConsentAsync(bool consent, CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .EndPoint("XAPI/User")
            .AddUrlSegment("DataProtectionConsent")
            .AddParameter("consent", consent)
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        return HandleResponse(result, r => System.Text.Json.JsonSerializer.Deserialize<UserViewModel>(r.GetStringContent(), GetJsonSerializerOptions()));
    }

    private static JsonSerializerOptions GetJsonSerializerOptions()
        => new JsonSerializerOptions().AddConverters(new JsonMessageConverter(), new JsonStringEnumConverter());
}
