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

        public async Task<bool> UploadFileAsync(int projectId, System.IO.FileInfo fileInfo,
            string language, bool transcribe,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateWebSocketsAsyncUnauthorizedPolicy<bool>();
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await WebSocketsApi.UploadFileAsync(projectId, language, transcribe, fileInfo, cancellationToken);
            }, CreatePollyContext(cancellationToken), cancellationToken));
        }
    }
}
