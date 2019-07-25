using Beey.DataExchangeModel.Files;
using Beey.DataExchangeModel.Projects;
using Beey.Client;
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

            var beey = new BeeyClient(beeyurl);
            await beey.LoginAsync(login, pass);

            var now = DateTime.Now;

            using var fs = File.Create(now.ToString("yyyy'-'MM'-'dd'T'HH'-'mm'-'ss") + "croplus128.mp3");
            using var msw = new StreamWriter(File.Create(now.ToString("yyyy'-'MM'-'dd'T'HH'-'mm'-'ss") + "croplus128.msgs"));
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

        static readonly CancellationTokenSource breaker = new CancellationTokenSource();
        public static async Task Listener(BeeyClient beey, Project proj, StreamWriter writer)
        {
            try
            {
                var messages = await beey.ListenToMessages(proj.Id, breaker.Token);
                breaker.CancelAfter(TimeSpan.FromMinutes(1));
                await foreach (var s in messages)
                {
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

        //TODO: logging stream is not logging...
        public static async Task<long> UploadStream(Stream data, BeeyClient beey, Project proj, Stream backup)
        {
            using LoggingStream ls = new LoggingStream(data);
            await beey.UploadStreamAsync(proj.Id, proj.AccessToken, "icecast.mp3", ls, null, "cz", true, breaker.Token);

            return ls.TotalRead;
        }

    }
}
