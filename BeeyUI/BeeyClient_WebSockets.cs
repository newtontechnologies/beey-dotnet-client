using Beey.Api;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Beey.Api.WebSockets;
using System.IO;

namespace BeeyUI
{
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

        public async Task<bool> UploadStreamAsync(int projectId, string dataName, Stream data,
            long? dataLength, string language, bool transcribe,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateWebSocketsAsyncUnauthorizedPolicy<bool>();
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await WebSocketsApi.UploadStreamAsync(projectId, dataName, data, dataLength, language, transcribe, cancellationToken);
            }, CreatePollyContext(cancellationToken), cancellationToken));
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
}
