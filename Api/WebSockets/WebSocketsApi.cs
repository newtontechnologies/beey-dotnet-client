using Beey.DataExchangeModel.Auth;
using Beey.DataExchangeModel.Files;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api.WebSockets
{
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

        public Task UploadStreamAsync(int projectId, string dataName, byte[] data, long? dataLength, bool saveMedia, CancellationToken cancellationToken)
        {
            using (var ms = new MemoryStream(data))
            {
                return UploadStreamAsync(projectId, dataName, ms, dataLength, saveMedia, cancellationToken);
            }
        }

        public async Task UploadStreamAsync(int projectId, string dataName, Stream data, long? dataLength, bool saveMedia, CancellationToken cancellationToken)
        {
            var policy = RetryPolicies.CreateAsyncNetworkPolicy<bool>(logger);
            bool res = await policy.ExecuteAsync(async (c) =>
            {
                var ws = await CreateBuilder()
                       .AddUrlSegment("Upload")
                       .AddParameter("id", projectId.ToString())
                       .AddParameter("saveMedia", saveMedia)
                       .OpenConnectionAsync(c);

                await Task.Delay(1000);

                long totalRead = 0;
                byte[] buffer = new byte[32 * 1024];
                var lastreporttime = DateTime.MinValue;
                var starttime = DateTime.Now;

                using (data)
                {
                    var (bytes, res) = await ws.ReceiveMessageAsync(buffer, c);
                    var fi = JsonConvert.DeserializeObject<FileStateInfo>(Encoding.UTF8.GetString(buffer, 0, bytes));
                    if (fi.BufferSize > buffer.Length)
                        buffer = new byte[fi.BufferSize];


                    fi = new FileStateInfo()
                    {
                        FileName = dataName,
                        TotalFileSize = dataLength,
                        BufferSize = buffer.Length,
                    };

                    await ws.SendAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(fi)), WebSocketMessageType.Text, true, c);


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

                    //initiate close handshake
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "file sent", c);
                    //wait for close from server
                    (bytes, res) = await ws.ReceiveMessageAsync(buffer, c);
                    if (res.MessageType != WebSocketMessageType.Close)
                        logger.Log(Logging.LogLevel.Warn, () => "data received after Websocket close handshake was intitiated");

                }

                return true;
            }, cancellationToken);
        }

        public async Task<IAsyncEnumerable<string>> ListenToMessages(int projectId, CancellationToken cancellationToken = default)
        {
            var policy = RetryPolicies.CreateAsyncNetworkPolicy<IAsyncEnumerable<string>>(logger);

            static async IAsyncEnumerable<string> receive(ClientWebSocket ws, CancellationToken c)
            {
                byte[] buffer = new byte[32 * 1024];
                (int bytes, ValueWebSocketReceiveResult result) res;
                do
                {
                    //TODO: receive async can receive only partial message...
                    res = await ws.ReceiveMessageAsync(buffer, c);
                    if (res.result.MessageType != WebSocketMessageType.Close)
                        yield return Encoding.UTF8.GetString(buffer, 0, res.bytes);

                    if (c.IsCancellationRequested)
                    {
                        await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", default);
                    }
                } while (res.bytes > 0 || res.result.MessageType != WebSocketMessageType.Close);
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
}
