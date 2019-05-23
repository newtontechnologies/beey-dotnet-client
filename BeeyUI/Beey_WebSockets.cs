using BeeyApi;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeeyApi.WebSockets;

namespace BeeyUI
{
    public partial class Beey
    {
        public async Task<string?> EchoAsync(string text,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateWebSocketsAsyncUnauthorizedPolicy<string?>(() => null);
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                var result = await WebSocketsApi.EchoAsync(text, cancellationToken);
                return (result, WebSocketsApi.LastCloseStatus);
            }, CreatePollyContext(cancellationToken), cancellationToken)).Result;
        }

        public async Task<string?> SpeakerSuggestionAsync(string search,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateWebSocketsAsyncUnauthorizedPolicy<string?>(() => null);
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                var result = await WebSocketsApi.SpeakerSuggestionAsync(search, cancellationToken);
                return (result, WebSocketsApi.LastCloseStatus);
            }, CreatePollyContext(cancellationToken), cancellationToken)).Result;
        }

        public async Task<bool?> UploadFileAsync(int projectId, System.IO.FileInfo fileInfo,
            string language, bool transcribe,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateWebSocketsAsyncUnauthorizedPolicy<bool?>(() => false);
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                var result = await WebSocketsApi.UploadFileAsync(projectId, language, transcribe, fileInfo, cancellationToken);
                return (result, WebSocketsApi.LastCloseStatus);
            }, CreatePollyContext(cancellationToken), cancellationToken)).Result;
        }
    }
}
