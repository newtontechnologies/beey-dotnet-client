using Beey.DataExchangeModel.Projects;
using System.Threading.Tasks;
using System.Threading;
using Beey.DataExchangeModel.Lexicons;
using System.Text.Json;
using System.Collections.Generic;
using System.Net.Http.Json;
using System;

namespace Beey.Client;

public partial class BeeyClient
{
    public async Task<LexiconEntryDto[]> V2_GetLexiconAsync(string language, CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<LexiconEntryDto[]>();
        return (await policy.ExecuteAsync(async (ctx, c) =>
        {
            var response = await CustomApi.CallAsync("XAPI/v2/CurrentTeam/Lexicon", [("language", language)], Api.Rest.HttpMethod.GET, true, cancellationToken);
            return V2Response.Deserialize<LexiconEntryDto[]>(response) ?? Array.Empty<LexiconEntryDto>();
        }, CreatePollyContext(cancellationToken), cancellationToken));
    }

    public async Task<string[]> V2_ListLexiconLanguagesAsync(CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<string[]>();
        return (await policy.ExecuteAsync(async (ctx, c) =>
        {
            var response = await CustomApi.CallAsync("XAPI/v2/CurrentTeam/Lexicon/ListLanguages", [], Api.Rest.HttpMethod.GET, true, cancellationToken);
            return V2Response.Deserialize<string[]>(response) ?? Array.Empty<string>();
        }, CreatePollyContext(cancellationToken), cancellationToken));
    }

    public async Task<LexiconEntryDto[]> V2_SetLexiconAsync(
        string language,
        IEnumerable<LexiconEntryDto>? lexicon,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<LexiconEntryDto[]>();
        return (await policy.ExecuteAsync(async (ctx, c) =>
        {
            var response = await CustomApi.CallAsync(
                $"XAPI/v2/CurrentTeam/Lexicon?Language={language}",
                JsonContent.Create(lexicon),
                Api.Rest.HttpMethod.POST,
                true,
                cancellationToken);

            return V2Response.Deserialize<LexiconEntryDto[]>(response) ?? Array.Empty<LexiconEntryDto>();
        }, CreatePollyContext(cancellationToken), cancellationToken));
    }

    public async Task<LexiconEntryDto[]> V2_GetLexiconAsync(int teamId, string language, CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<LexiconEntryDto[]>();
        return (await policy.ExecuteAsync(async (ctx, c) =>
        {
            var response = await CustomApi.CallAsync($"XAPI/v2/Admin/Teams/{teamId}/Lexicon", [("language", language)], Api.Rest.HttpMethod.GET, true, cancellationToken);
            return V2Response.Deserialize<LexiconEntryDto[]>(response) ?? Array.Empty<LexiconEntryDto>();
        }, CreatePollyContext(cancellationToken), cancellationToken));
    }

    public async Task<string[]> V2_ListLexiconLanguagesAsync(int teamId, CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<string[]>();
        return (await policy.ExecuteAsync(async (ctx, c) =>
        {
            var response = await CustomApi.CallAsync($"XAPI/v2/Admin/Teams/{teamId}/Lexicon/ListLanguages", [], Api.Rest.HttpMethod.GET, true, cancellationToken);
            return V2Response.Deserialize<string[]>(response) ?? Array.Empty<string>();
        }, CreatePollyContext(cancellationToken), cancellationToken));
    }

    public async Task<LexiconEntryDto[]> V2_SetLexiconAsync(
        int teamId,
        string language,
        IEnumerable<LexiconEntryDto>? lexicon,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<LexiconEntryDto[]>();
        return (await policy.ExecuteAsync(async (ctx, c) =>
        {
            var response = await CustomApi.CallAsync(
                $"XAPI/v2/Admin/Teams/{teamId}/Lexicon?Language={language}",
                JsonContent.Create(lexicon),
                Api.Rest.HttpMethod.POST,
                true,
                cancellationToken);

            return V2Response.Deserialize<LexiconEntryDto[]>(response) ?? Array.Empty<LexiconEntryDto>();
        }, CreatePollyContext(cancellationToken), cancellationToken));
    }
}
