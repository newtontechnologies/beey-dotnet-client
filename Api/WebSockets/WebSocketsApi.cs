using Beey.DataExchangeModel.Auth;
using Beey.DataExchangeModel.Files;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api.WebSockets;

public class WebSocketsApi
{
    private readonly ILogger<WebSocketsApi> logger = LoggerFactoryProvider.LoggerFactory.CreateLogger<WebSocketsApi>();

    public LoginToken? Token { get; set; }
    private string url;

    public WebSocketsApi(string url)
    {
        this.url = url;
    }

    internal WebSocketMessageBuilder CreateBuilder()
    {
        if (Token == null) { throw new UnauthorizedAccessException(); }

        return new WebSocketMessageBuilder(this.url)
            .AddUrlSegment("ws")
            .AddParameter("Authorization", this.Token.Token);
    }

    public async Task<string> SpeakerSuggestionAsync(string search, CancellationToken cancellationToken)
    {
        var policy = RetryPolicies.CreateAsyncNetworkPolicy<string>(logger);
        string res = await policy.ExecuteAsync(async c =>
        {
            byte[] buffer = new byte[32 * 1024];
            var ws = await CreateBuilder()
            .AddUrlSegment("SpeakerSuggestion")
            .OpenConnectionAsync(c);

            await ws.SendAsync(Encoding.UTF8.GetBytes(search), WebSocketMessageType.Text, true, cancellationToken);
            var result = await ws.ReceiveMessageAsync(buffer, c);

            return Encoding.UTF8.GetString(buffer, 0, result.bytes);
        }, cancellationToken);

        return res;
    }

    public async Task<string> EchoAsync(string text, CancellationToken cancellationToken)
    {
        var policy = RetryPolicies.CreateAsyncNetworkPolicy<string>(logger);
        string res = await policy.ExecuteAsync(async (c) =>
        {
            byte[] buffer = new byte[32 * 1024];
            var ws = await CreateBuilder()
                    .AddUrlSegment("Echo")
                    .OpenConnectionAsync(c);

            await ws.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, cancellationToken);
            var result = await ws.ReceiveMessageAsync(buffer, c);

            return Encoding.UTF8.GetString(buffer, 0, result.bytes);

        }, cancellationToken);
        return res;
    }

    public async Task UploadStreamAsync(int projectId, string dataName, byte[] data, long? dataLength, bool saveMedia, string transcodingProfile, CancellationToken cancellationToken)
    {
        using (var ms = new MemoryStream(data))
        {
            await UploadStreamAsync(projectId, dataName, ms, dataLength, saveMedia, transcodingProfile, cancellationToken);
        }
    }

    public async Task UploadStreamAsync(int projectId, string dataName, Stream data, long? dataLength, bool saveMedia, string transcodingProfile, CancellationToken cancellationToken)
    {
        var policy = RetryPolicies.CreateAsyncNetworkPolicy<bool>(logger);
        bool res = await policy.ExecuteAsync(async (c) =>
        {
            var ws = await CreateBuilder()
                   .AddUrlSegment("Upload")
                   .AddParameter("id", projectId.ToString())
                   .AddParameter("saveMedia", saveMedia)
                   .AddParameter("transcodingProfile", transcodingProfile)
                   .OpenConnectionAsync(c);

            await Task.Delay(1000);

            byte[] buffer = new byte[32 * 1024];
            var starttime = DateTime.Now;

            var (bytes, res) = await ws.ReceiveMessageAsync(buffer, c);
            var fi = JsonSerializer.Deserialize<FileStateInfo>(Encoding.UTF8.GetString(buffer, 0, bytes));
            if (fi.BufferSize > buffer.Length)
                buffer = new byte[fi.BufferSize];

            fi = new FileStateInfo()
            {
                FileName = dataName,
                TotalFileSize = dataLength,
                BufferSize = buffer.Length,
            };

            await ws.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(fi)), WebSocketMessageType.Text, true, c);

            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var receivingUntilClosed = Task.Run(async () =>
            {
                byte[] rcvBuffer = new byte[32 * 1024];
                ValueWebSocketReceiveResult? lastResult = null;
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        lastResult = (await ws.ReceiveMessageAsync(rcvBuffer, default)).lastResult;
                        if (lastResult.Value.MessageType == WebSocketMessageType.Close)
                            break;
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
                return lastResult;
            }, token);

            //send chunked
            var lastReportTime = DateTime.MinValue;
            long totalLength = 0;
            using (var ms = new MemoryStream(buffer))
            using (var bw = new BinaryWriter(ms))
            {
                while (true)
                {
                    const int hdrLength = sizeof(double) + sizeof(short);

                    var bodyLength = await data.ReadAsync(buffer, hdrLength, buffer.Length - hdrLength);
                    if (bodyLength <= 0) //EOF
                        break;

                    ms.Seek(0, SeekOrigin.Begin);
                    bw.Write((double)totalLength); // TODO: use UInt64 instead of double
                    bw.Write((short)bodyLength);

                    await ws.SendAsync(new ArraySegment<byte>(buffer, 0, hdrLength + bodyLength), WebSocketMessageType.Binary, true, c);

                    totalLength += bodyLength;

                    var tdelta = DateTime.Now - lastReportTime;
                    if (tdelta > TimeSpan.FromSeconds(10))
                    {
                        logger.LogInformation("written: {totalRead}B seconds: {seconds}:", totalLength, DateTime.Now - starttime);
                        lastReportTime = DateTime.Now;
                    }
                }
            }

            // initiate close handshake
            await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "file sent", c);

            //TODO: rewrite closing correctly (gitlab issue #6)
            // This is Race condition, There can be messages in transfer, when Close is initiated.
            //BUT receivingUntilClosed will close immediately on CT and (possibly) not even read all buffered messages..
            cts.Cancel(); // stop listening for new messages
            // wait for close from server
            var lastResult = await receivingUntilClosed ?? (await ws.ReceiveMessageAsync(buffer, default)).lastResult;

            //this is here probably to fix the race.. to receive messages that were in transfer before server acknowledges close..
            while (lastResult.MessageType != WebSocketMessageType.Close)
            {
                //data after close is quite ordinary because of the race condition...
                logger.LogInformation("data received after Websocket close handshake was intitiated");
                lastResult = (await ws.ReceiveMessageAsync(buffer, default)).lastResult;
            }

            return true;
        }, cancellationToken);
    }

    public async Task<IAsyncEnumerable<string>> ListenToMessages(int projectId, CancellationToken cancellationToken)
    {
        var policy = RetryPolicies.CreateAsyncNetworkPolicy<IAsyncEnumerable<string>>(logger);

        static async IAsyncEnumerable<string> receive(ClientWebSocket ws, [EnumeratorCancellation] CancellationToken c)
        {
            try
            {
                byte[] buffer = new byte[1024 * 1024];
                int count = 0;
                (int bytes, ValueWebSocketReceiveResult result) res;

                do
                {
                    c.ThrowIfCancellationRequested();
                    res = await ws.ReceiveMessageAsync(buffer.AsMemory(count), c);
                    count += res.bytes;
                    if (res.result.EndOfMessage)
                    {
                        if (res.result.MessageType != WebSocketMessageType.Close)
                            yield return Encoding.UTF8.GetString(buffer.AsSpan(..count));

                        count = 0;
                    }
                } while (res.bytes > 0 || res.result.MessageType != WebSocketMessageType.Close);
            }
            finally
            {
                if (ws.State == WebSocketState.Open || ws.State == WebSocketState.Connecting)
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", default);
            }
        }


        IAsyncEnumerable<string> res = await policy.ExecuteAsync(async (c) =>
        {
            var ws = await CreateBuilder()
                   .AddUrlSegment("LiveUpdate")
                   .AddParameter("projectid", projectId.ToString())
                   .OpenConnectionAsync(c);
            return receive(ws, c);
        }, cancellationToken);

        return res;

    }
}
