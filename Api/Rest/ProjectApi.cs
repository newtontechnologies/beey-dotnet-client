﻿using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Auth;
using Beey.DataExchangeModel.Messaging;
using Beey.DataExchangeModel.Projects;
using Beey.DataExchangeModel.Serialization.JsonConverters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api.Rest;

/// <summary>
/// All methods:
/// 1) returns result if everything is ok,
/// 2) throws exception when there is some network or framework problem,
/// 3a) throws exception when server returned 500
/// 3b) returns null if everything is ok, but backend returned error.
///     Description of the error is then in the properties LastError and LastHttpStatusCode.
/// </summary>
public partial class ProjectApi : BaseAuthApi<ProjectApi>
{
    public ProjectApi(string url) : base(url)
    {
        EndPoint = "XAPI/Project";
    }

    public async Task<Project> CreateAsync(string name, string customPath,
        CancellationToken cancellationToken)
    {
        return await CreateAsync(new ParamsProjectInit()
        {
            Name = name,
            CustomPath = customPath
        }, cancellationToken);
    }

    public async Task<Project> CreateAsync(ParamsProjectInit init,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .SetBody(JsonSerializer.Serialize(init), "application/json")
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<Project>(r.GetStringContent()));
    }

    public async Task<Project> GetAsync(int id,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(id.ToString())
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<Project>(r.GetStringContent()));
    }

    public async Task<Project> UpdateAsync(Project project,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(project.Id.ToString())
            .SetBody(JsonSerializer.Serialize(project, new JsonSerializerOptions() { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }), "application/json")
            .ExecuteAsync(HttpMethod.PUT, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<Project>(r.GetStringContent()));
    }

    public async Task<Project> UpdateAsync(int id, long accessToken, Dictionary<string, object> properties,
       CancellationToken cancellationToken)
    {
        if (!properties.ContainsKey("Id") && !properties.ContainsKey("id"))
        {
            properties.Add("Id", id);
        }
        if (!properties.ContainsKey("AccessToken") && !properties.ContainsKey("accessToken"))
        {
            properties.Add("AccessToken", accessToken);
        }

        var result = await CreateBuilder()
            .AddUrlSegment(id.ToString())
            .SetBody(JsonSerializer.Serialize(properties, new JsonSerializerOptions() { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }), "application/json")
            .ExecuteAsync(HttpMethod.PUT, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<Project>(r.GetStringContent()));
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
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

    public async Task ResetAsync(int id, CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(id.ToString())
            .AddUrlSegment("Reset")
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        HandleResponse(result);
    }

    public async Task<Listing<ProjectAccessViewModel>> ListProjectsAsync(int count, int skip,
        OrderOn orderOn, bool ascending, DateTime? from, DateTime? to,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("List")
            .AddParameter("skip", skip)
            .AddParameter("count", count)
            .AddParameter("orderOn", GetOrderOn(orderOn))
            .AddParameter("orderBy", ascending ? "ascending" : "descending")
            .AddParameter("from", from?.ToString("s"))
            .AddParameter("to", to?.ToString("s"))
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<Listing<ProjectAccessViewModel>>(r.GetStringContent()));
    }       

    public async Task<Project> TranscribeProjectAsync(int projectId, string language,
        bool withPpc, bool withVad, bool withPunctuation, bool withSpeakerId, bool withDiarization, bool saveTrsx,
        string transcriptionProfile,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(projectId.ToString())
            .AddUrlSegment("Transcribe")
            .AddParameter("lang", language)
            .AddParameter("withPPC", withPpc)
            .AddParameter("withVAD", withVad)
            .AddParameter("withSpeakerId", withSpeakerId)
            .AddParameter("withDiarization", withDiarization)
            .AddParameter("saveTrsx", saveTrsx)
            .AddParameter("withPunctuation", withPunctuation)
            .AddParameter("transcriptionProfile", transcriptionProfile)
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<Project>(r.GetStringContent()));
    }

    public async Task<Project> EnqueueProjectAsync(int projectId, string language,
        bool withPpc, bool withVad, bool withPunctuation, bool withSpeakerId, bool withDiarization, bool saveTrsx,
        string transcriptionProfile,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("Queue/Enqueue")
            .AddParameter("projectId", projectId)
            .AddParameter("lang", language)
            .AddParameter("withPPC", withPpc)
            .AddParameter("withVAD", withVad)
            .AddParameter("withSpeakerId", withSpeakerId)
            .AddParameter("withDiarization", withDiarization)
            .AddParameter("saveTrsx", saveTrsx)
            .AddParameter("withPunctuation", withPunctuation)
            .AddParameter("transcriptionProfile", transcriptionProfile)
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<Project>(r.GetStringContent()));
    }

    public async Task<Message[]> GetMessagesAsync(int id, DateTime? from, CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(id.ToString())
            .AddUrlSegment("MessageCache")
            .AddParameter("from", from)
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<Message[]>(r.GetStringContent(), GetDefaultJsonSerializerOptions()));
    }

    public async Task<Project> CopyProjectAsync(int id, CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(id.ToString())
            .AddUrlSegment("Copy")
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<Project>(r.GetStringContent()));
    }

    public enum OrderOn { Created, Updated, None }
    private static string GetOrderOn(OrderOn orderOn)
    {
        return orderOn switch
        {
            OrderOn.Updated => "updated",
            _ => "created",
        };
    }

    private static JsonSerializerOptions GetDefaultJsonSerializerOptions() => Message.DefaultJsonSerializerOptions;
}
