using BeeyApi.POCO.Auth;
using BeeyApi.POCO.Files;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeeyApi.WebSockets
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
            var policy = RetryPolicies.CreateAsyncNetworkPolicy(() => default(string), LogException, logger);
            string res = await policy.ExecuteAsync(async (c) =>
            {
                OpenedWebSocket ws = await CreateBuilder()
                .AddUrlSegment("SpeakerSuggestion")
                .OpenConnectionAsync(c);

                await ws.SendAsync(Encoding.UTF8.GetBytes(search), WebSocketMessageType.Text, true, cancellationToken);
                var result = await ws.ReceiveAsync(2048, c);

                return Encoding.UTF8.GetString(result);
            }, cancellationToken);

            return res;
        }

        public async Task<string> EchoAsync(string text, CancellationToken cancellationToken)
        {
            var policy = RetryPolicies.CreateAsyncNetworkPolicy(() => default(string), LogException, logger);
            string res = await policy.ExecuteAsync(async (c) =>
            {
                OpenedWebSocket ws = await CreateBuilder()
                        .AddUrlSegment("Echo")
                        .OpenConnectionAsync(c);

                await ws.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, cancellationToken);
                var result = await ws.ReceiveAsync(2048, c);

                return Encoding.UTF8.GetString(result);

            }, cancellationToken);
            return res;
        }

        public async Task<bool> UploadFileAsync(int projectId, string language, bool transcribe, FileInfo file, CancellationToken cancellationToken)
        {
            var policy = RetryPolicies.CreateAsyncNetworkPolicy(() => false, LogException, logger);
            bool res = await policy.ExecuteAsync(async (c) =>
            {
                OpenedWebSocket ws = await CreateBuilder()
                       .AddUrlSegment("Upload")
                       .AddParameter("id", projectId.ToString())
                       .AddParameter("lang", language)
                       .AddParameter("transcribe", transcribe.ToString().ToLower())
                       .OpenConnectionAsync(c);

                await Task.Delay(1000);

                int bufferSize = 4096;

                using (var s = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var res = await ws.ReceiveAsync(bufferSize, c);

                    for (int i = 0; i < 10 && res.Length == 0; i++)
                    {
                        await Task.Delay(1000);
                        res = await ws.ReceiveAsync(bufferSize, c);
                    }

                    var fi = JsonConvert.DeserializeObject<FileStateInfo>(Encoding.UTF8.GetString(res));
                    byte[] buffer = new byte[fi.BufferSize];


                    fi = new FileStateInfo()
                    {
                        FileName = file.Name,
                        TotalFileSize = (int)file.Length,
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
                            bw.Write((double)s.Position);

                            var read = s.Read(buffer, sizeof(double) + sizeof(short), buffer.Length - sizeof(double) - sizeof(short));
                            if (read <= 0) //EOF
                                break;


                            ms.Seek(sizeof(double), SeekOrigin.Begin);
                            bw.Write((short)read);

                            await ws.SendAsync(new ArraySegment<byte>(buffer, 0, sizeof(double) + sizeof(short) + read), WebSocketMessageType.Binary, true, c);
                        }
                    }
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "file sent", c);
                }

                return true;
            }, cancellationToken);

            return res;
        }

        private void LogException(Exception ex)
        {
            if (ex is WebSocketClosedException wsEx && wsEx.CloseStatus.HasValue)
            {
                logger.Log(Logging.LogLevel.Error, () => $"WebSocket closed ({wsEx.CloseStatus?.ToString()}) with message '{wsEx.Message}'.", ex);
            }
            else
            {
                logger.Log(Logging.LogLevel.Error, () => ex.Message, ex);
            }
        }
    }
}
