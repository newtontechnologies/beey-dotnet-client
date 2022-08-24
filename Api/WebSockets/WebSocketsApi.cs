using Beey.Api.Logging;
using Beey.DataExchangeModel.Auth;
using Beey.DataExchangeModel.Files;
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
    private Logging.ILog logger = Logging.LogProvider.For<WebSocketsApi>();

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

            long totalRead = 0;
            byte[] buffer = new byte[32 * 1024];
            var lastreporttime = DateTime.MinValue;
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
                byte[] buffer = new byte[32 * 1024];
                ValueWebSocketReceiveResult? lastResult = null;
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        lastResult = (await ws.ReceiveMessageAsync(buffer, default)).lastResult;
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
            using (MemoryStream ms = new MemoryStream(buffer))
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                while (true)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    bw.Write((double)totalRead);

                    var read = await data.ReadAsync(buffer, sizeof(double) + sizeof(short), buffer.Length - sizeof(double) - sizeof(short));
                    if (read <= 0) //EOF
                        break;

                    ms.Seek(sizeof(double), SeekOrigin.Begin);
                    bw.Write((short)read);

                    await ws.SendAsync(new ArraySegment<byte>(buffer, 0, sizeof(double) + sizeof(short) + read), WebSocketMessageType.Binary, true, c);

                    totalRead += read;

                    var tdelta = DateTime.Now - lastreporttime;
                    if (tdelta > TimeSpan.FromSeconds(10))
                    {
                        logger.Log(Logging.LogLevel.Info, () => $"written: {totalRead}B seconds: {DateTime.Now - starttime}:");
                        lastreporttime = DateTime.Now;
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
                logger.Info("data received after Websocket close handshake was intitiated");
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
                byte[] buffer = new byte[32 * 1024];
                (int bytes, ValueWebSocketReceiveResult result) res;
                do
                {
                    c.ThrowIfCancellationRequested();
                    //TODO: receive async can receive only partial message...
                    res = await ws.ReceiveMessageAsync(buffer, c);
                    if (res.result.MessageType != WebSocketMessageType.Close)
                        yield return Encoding.UTF8.GetString(buffer, 0, res.bytes);
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
