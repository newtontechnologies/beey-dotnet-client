using BeeyApi.POCO.Files;
using BeeyApi.POCO.Projects;
using BeeyUI;
using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StreamPusher
{
    class Program
    {
        static readonly ILogger _logger = Log.ForContext<Program>();
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("pusher.log")
            .CreateLogger();

            string beeyurl = args[0];
            string login = args[1];
            string pass = args[2];
            int projectid = int.Parse(args[3]);
            string dataurl = args[4];

            var beey = new Beey(beeyurl);
            string err = await beey.LoginAsync(login, pass);

            var now = DateTime.Now;
            using (var fs = File.Create(now.ToString("yyyy'-'MM'-'dd'T'HH'-'mm'-'ss") + "croplus128.mp3"))
            using (var msw = new StreamWriter(File.Create(now.ToString("yyyy'-'MM'-'dd'T'HH'-'mm'-'ss") + "croplus128.msgs")))
            {
                var p = await beey.GetProjectAsync(projectid);

                if (p == null)
                    p = await beey.CreateProjectAsync(new ParamsProjectInit() { Name = "icecast ČRO+", CustomPath = "ČRO+/" });

                _logger.Information("Created project {@project}", p);

                HttpClient downloader = new HttpClient();
                var watchdog = Listener(beey, p, msw);
                var data = await downloader.GetStreamAsync(dataurl);
                _logger.Information("downloading file:{stream}", dataurl);
                var written = await UploadStream(data, beey, p, fs);

                _logger.Information("Upload stopped, bytes written: {written}", written);
            }
        }

        static readonly CancellationTokenSource breaker = new CancellationTokenSource();
        public static async Task Listener(Beey beey, Project proj, StreamWriter writer)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(2));
                var endpoint = beey.Url.Replace("http://", "ws://").Replace("http://", "wss://");
                endpoint = $"{endpoint.TrimEnd('/')}/ws/LiveUpdate?Authorization={beey.LoginToken.Token}&projectid={proj.Id}";
                var ws = new ClientWebSocket();
                await ws.ConnectAsync(new Uri(endpoint), breaker.Token);

                byte[] buffer = new byte[32 * 1024];

                var res = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), breaker.Token);
                breaker.CancelAfter(TimeSpan.FromMinutes(1));
                while (res.CloseStatus == null)
                {

                    res = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), breaker.Token);
                    var s = Encoding.UTF8.GetString(buffer, 0, res.Count);
                    await writer.WriteLineAsync(s);
                    if (!s.Contains("FileOffset") && s.Length > 5)
                        breaker.CancelAfter(TimeSpan.FromMinutes(1));

                }

            }
            catch
            {

            }
            breaker.Cancel();
        }


        public static async Task<long> UploadStream(Stream data, Beey beey, Project proj, Stream backup)
        {
            long totalRead = 0;
            var starttime = DateTime.Now;
            var lastreporttime = DateTime.MinValue;
            try
            {
                var endpoint = beey.Url.Replace("http://", "ws://").Replace("http://", "wss://");
                var ws = new ClientWebSocket();

                endpoint = $"{endpoint.TrimEnd('/')}/ws/Upload?Authorization={beey.LoginToken.Token}&id={proj.Id}&lang=cz&transcribe=true&saveTrsx=false&saveMedia=false";
                await ws.ConnectAsync(new Uri(endpoint), default);
                await Task.Delay(1000);
                byte[] buffer = new byte[32 * 1024];

                using (data)
                {
                    var res = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), breaker.Token);

                    for (int i = 0; i < 10 && res.Count == 0; i++)
                    {
                        await Task.Delay(1000);
                        res = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), breaker.Token);
                    }

                    var fi = JsonConvert.DeserializeObject<FileStateInfo>(Encoding.UTF8.GetString(buffer, 0, res.Count));
                    buffer = new byte[fi.BufferSize];


                    fi = new FileStateInfo()
                    {
                        FileName = "icecastupload.mp3",
                        TotalFileSize = null,
                        BufferSize = buffer.Length,
                    };

                    await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(fi))), WebSocketMessageType.Text, true, breaker.Token);



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

                            await backup.WriteAsync(buffer, sizeof(double) + sizeof(short), read);


                            ms.Seek(sizeof(double), SeekOrigin.Begin);
                            bw.Write((short)read);

                            await ws.SendAsync(new ArraySegment<byte>(buffer, 0, sizeof(double) + sizeof(short) + read), WebSocketMessageType.Binary, true, breaker.Token);

                            totalRead += read;

                            var tdelta = DateTime.Now - lastreporttime;
                            if (tdelta > TimeSpan.FromSeconds(10))
                            {
                                _logger.Information("written: {bytes}B seconds: {seconds}:", totalRead, DateTime.Now - starttime);
                                lastreporttime = DateTime.Now;
                            }
                        }
                    }
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "stream send done", default);
                }
                return totalRead;

            }
            catch (Exception e)
            {
                _logger.Fatal(e, "connection to stream failed");
                breaker.Cancel();
                return totalRead;
            }
        }

    }
}
