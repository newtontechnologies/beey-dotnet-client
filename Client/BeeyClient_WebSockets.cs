﻿using Beey.Api;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Beey.Api.WebSockets;
using System.IO;

namespace Beey.Client;

public partial class BeeyClient
{
    public async Task<string> EchoAsync(string text,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateWebSocketsAsyncUnauthorizedPolicy<string>();
        return (await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await WebSocketsApi.EchoAsync(text, cancellationToken);
        }, CreatePollyContext(cancellationToken), cancellationToken));
    }

    public async Task<string> SpeakerSuggestionAsync(string search,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateWebSocketsAsyncUnauthorizedPolicy<string>();
        return (await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await WebSocketsApi.SpeakerSuggestionAsync(search, cancellationToken);
        }, CreatePollyContext(cancellationToken), cancellationToken));
    }

    public async Task UploadStreamAsync(int projectId, string dataName, Stream data,
        long? dataLength, bool saveMedia,
        string transcodingProfile = "default",
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateWebSocketsAsyncUnauthorizedPolicy<bool>();
        await policy.ExecuteAsync(async (ctx, c) =>
        {
            await WebSocketsApi.UploadStreamAsync(projectId, dataName, data, dataLength, saveMedia, transcodingProfile, cancellationToken);
            return true;
        }, CreatePollyContext(cancellationToken), cancellationToken);
    }

    public async Task<IAsyncEnumerable<string>> ListenToMessages(int projectId,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();
        var policy = CreateWebSocketsAsyncUnauthorizedPolicy<IAsyncEnumerable<string>>();
        var it = await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await WebSocketsApi.ListenToMessages(projectId, cancellationToken);
        }, CreatePollyContext(cancellationToken), cancellationToken);

        return it;
    }
}
