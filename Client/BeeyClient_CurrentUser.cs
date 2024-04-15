using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Auth;
using Beey.DataExchangeModel.Lexicons;
using Beey.DataExchangeModel.Messaging;
using Beey.DataExchangeModel.Projects;
using Beey.DataExchangeModel.Users;

namespace Beey.Client;

public partial class BeeyClient
{
    public async Task ChangePasswordAsync(string oldPassword, string newPassword,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
        await policy.ExecuteAsync(async (ctx, c) =>
        {
            await CurrentUserApi.ChangePasswordAsync(oldPassword, newPassword, cancellationToken);
            return true;
        }, CreatePollyContext(cancellationToken), cancellationToken);
    }

    public async Task<LoginToken> GetUserInfoAsync(CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<LoginToken>();
        return await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await CurrentUserApi.GetUserInfoAsync(cancellationToken);
        }, CreatePollyContext(cancellationToken), cancellationToken);
    }

    public async Task<JsonObject> GetUserSettingsAsync(CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<JsonObject>();
        return await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await CurrentUserApi.GetUserSettingsAsync(cancellationToken);
        }, CreatePollyContext(cancellationToken), cancellationToken);
    }

    public async Task PostUserSettingsAsync(string settings,
        CancellationToken cancellationToken = default)
    {
        var jSettings = (JsonObject)JsonNode.Parse(settings)!;
        await PostUserSettingsAsync(jSettings, cancellationToken);
    }

    public async Task PostUserSettingsAsync(JsonObject settings,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
        await policy.ExecuteAsync(async (ctx, c) =>
        {
            await CurrentUserApi.PostUserSettingsAsync(settings, cancellationToken);
            return true;
        }, CreatePollyContext(cancellationToken), cancellationToken);
    }

    public async Task<LexiconEntryDto[]> GetUserLexAsync(string language, CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<LexiconEntryDto[]>();
        return await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await CurrentUserApi.GetUserLexAsync(language, cancellationToken);
        }, CreatePollyContext(cancellationToken), cancellationToken);
    }

    public async Task<List<string>> ListUserLexLanguagesAsync(CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<List<string>>();
        return await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await CurrentUserApi.ListUserLexLanguagesAsync(cancellationToken);
        }, CreatePollyContext(cancellationToken), cancellationToken);
    }

    public async Task SetUserLexAsync(string language, IEnumerable<LexiconEntryDto> userLex, CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
        await policy.ExecuteAsync(async (ctx, c) =>
        {
            await CurrentUserApi.SetUserLexAsync(language, userLex, cancellationToken);
            return true;
        }, CreatePollyContext(cancellationToken), cancellationToken);
    }

    public async Task<Message[]> GetUserMessagesAsync(DateTime? from, CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<Message[]>();
        return await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await CurrentUserApi.GetUserMessagesAsync(from, cancellationToken);
        }, CreatePollyContext(cancellationToken), cancellationToken);
    }

    public async Task<AggregatedListing<TranscriptionLogItemDto, decimal>> GetTranscriptionLogAsync(CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<AggregatedListing<TranscriptionLogItemDto, decimal>>();
        return await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await CurrentUserApi.GetTranscriptionLogAsync(cancellationToken);
        }, CreatePollyContext(cancellationToken), cancellationToken);
    }

    public async Task<PaymentInfoAddDto> GetPaymentInfoAsync(CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<PaymentInfoDto>();
        return await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await CurrentUserApi.GetPaymentInfoAsync(cancellationToken);
        }, CreatePollyContext(cancellationToken), cancellationToken);
    }

    public async Task SetPaymentInfoAsync(PaymentInfoDto paymentInfo, CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
        await policy.ExecuteAsync(async (ctx, c) =>
        {
            await CurrentUserApi.SetPaymentInfoAsync(paymentInfo, cancellationToken);
            return true;
        }, CreatePollyContext(cancellationToken), cancellationToken);
    }

    public async Task<bool> GetDataProtectionConsentAsync(CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
        return await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await CurrentUserApi.GetDataProtectionConsentAsync(cancellationToken);
        }, CreatePollyContext(cancellationToken), cancellationToken);
    }

    public async Task<UserDto> SetDataProtectionConsentAsync(bool consent, CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<UserDto>();
        return await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await CurrentUserApi.SetDataProtectionConsentAsync(consent, cancellationToken);
        }, CreatePollyContext(cancellationToken), cancellationToken);
    }
}
