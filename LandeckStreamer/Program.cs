using CommandLine;
using Common;
using LandeckTranscriber;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Beey.Client;
using Beey.DataExchangeModel.Projects;
using System.Threading;

namespace LandeckStreamer
{
    public class Program
    {
        private static ILogger _logger;

        public static BeeyConfiguration Configuration { get; private set; }
        public static bool Finished { get; private set; }

        static readonly DateTime ProgramStarted = DateTime.UtcNow;
        static readonly CancellationTokenSource breaker = new CancellationTokenSource();

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
               .Enrich.With(new ProcessIdEnricher())
               .WriteTo.File("LandeckStreamer.log", rollingInterval: RollingInterval.Day, shared: true, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {ProcessId} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
               .WriteTo.Console()
               .CreateLogger();

            _logger = Log.ForContext<Program>();

            _logger.Information("Starting with Args {@Args}", string.Join(" ", args));

            var conf = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddXmlFile("Settings.xml", optional: false)
                .Build();

            Configuration = conf.GetSection("Beey").Get<BeeyConfiguration>();

            CommandLine.Parser.Default.ParseArguments<CommandLineOptions>(args)
            .WithParsed(opts =>
            {
                try
                {
                    MainAsync(opts).Wait();
                }
                catch (AggregateException ae) when (ae.InnerException is InvalidOperationException iox && iox.Message == "stuck reloading same list...")
                {
                    _logger.Fatal(iox, "streamer failed with undrecoverable Landeck download error");
                    Log.CloseAndFlush();
                }
                catch (Exception e)
                {
                    _logger.Fatal(e, "streamer failed with undrecoverable error");
                    Log.CloseAndFlush();
                }
            });
            // .WithNotParsed((errs) => HandleParseError(errs));

            _logger.Information("Terminating sucessfuly");
        }

        private static async Task MainAsync(CommandLineOptions opts)
        {
            string projectname = opts.Title;
            string urlbase = null;
            bool IsVideo = false;
            JObject channelInfo = LandeckApi.LoadLandeckInfo(opts, opts.Start, opts.Length, ref urlbase, ref IsVideo);


            var videodownloader = IsVideo ? new MpdAudioDownloader(urlbase, opts.Title + Path.GetRandomFileName() + "video.mp4", "video/mp4") : null;
            var audiodownloader = new MpdAudioDownloader(urlbase, opts.Title + Path.GetRandomFileName() + "audio.mp4", "audio/mp4");


            var videoDownloadTask = IsVideo ? videodownloader.DownloadStream(opts.Start, opts.Length, null) : null;
            var audioDownloadTask = audiodownloader.DownloadStream(opts.Start, opts.Length, null);

            Process ffmpeg = Process.Start(new ProcessStartInfo()
            {
                FileName = Configuration.FFmpeg,
                Arguments = $"-y -re -rw_timeout 30000000 -follow 1 -seekable 0 -i \"{audiodownloader.FilePath}\" -rw_timeout 30000000 -follow 1 -seekable 0 -i \"{videodownloader.FilePath}\" -map 0:a:0 -map 1:v:0 -s 640x480 -c:a aac -b:a 64k -f ismv -",
                RedirectStandardOutput = true,

            });


            //var outtask = ffmpeg.StandardOutput.BaseStream.CopyToAsync(File.Create("muxed.mp4"));


            var beeyurl = Configuration.URL;
            var login = Configuration.Login;
            var pass = Configuration.Password;
            _logger.Information("logging into beey");
            var beey = new BeeyClient(beeyurl);
            await beey.LoginAsync(login, pass);
            _logger.Information("logged in");

            var now = DateTime.Now;
            StreamWriter msw = null;
            if (Configuration.LogMessages)//Start.Value.ToString("yyyy'-'MM'-'dd'T'HH'-'mm'-'ss")
                msw = new StreamWriter(File.Create($"pusher{ProgramStarted:yyyy'-'MM'-'dd'T'HH'-'mm'-'ss}.msgs")) { AutoFlush = true };

            var p = await beey.CreateProjectAsync(new ParamsProjectInit() { Name = projectname, CustomPath = projectname });


            _logger.Information("Created project {name} {@project}", projectname, p);

            var watchdog = Listener(beey, p, msw);
            await Task.Delay(TimeSpan.FromSeconds(2));

            _logger.Information("upload started");

            var upload = beey.UploadStreamAsync(p.Id, "sejm", ffmpeg.StandardOutput.BaseStream, null, true, breaker.Token);
            bool repeat = true;
            while (repeat)
            {
                try
                {

                    p = await beey.TranscribeProjectAsync(p.Id, Configuration.TranscriptionLocale, cancellationToken: breaker.Token);
                    repeat = false;
                }
                catch (Exception e)
                {

                }
            }
            await upload;


            _logger.Information("transcription finished");
            await Task.WhenAll(audioDownloadTask, videoDownloadTask, upload, watchdog);
        }




        public static async Task Listener(BeeyClient beey, Project proj, StreamWriter writer)
        {
            try
            {
                if (writer != null)
                {
                    await writer.WriteAsync('[');
                    await writer.WriteLineAsync();
                }
                _logger.Information("Opening websocket to listen to beey messages");
                var messages = await beey.ListenToMessages(proj.Id, breaker.Token);

                _logger.Information("Listening connected");
                await foreach (var s in messages)
                {
                    if (Configuration.MessageEcho)
                        Console.WriteLine(s);
                    if (writer != null)
                    {
                        await writer.WriteAsync(s);
                        await writer.WriteAsync(',');
                        await writer.WriteLineAsync();
                    }

                    if (s.Contains("Failed"))
                    {
                        _logger.Error("server reported processing error {message}", s);
                    }

                    if (s.Contains("RecognitionMsg") && !s.Contains("Started"))
                    {
                        _logger.Information("transcription ended on server with message {message}", s);
                        Finished = true;
                        breaker.Cancel();
                    }


                }
            }
            catch (Exception e)
            {
                if (e is TaskCanceledException && !Finished)
                    _logger.Error(e, "Listening to beey messages failed");
            }
            finally
            {
                if (writer != null)
                {
                    await writer.WriteLineAsync();
                    await writer.WriteAsync(']');
                    writer.Close();
                }

                if (!breaker.IsCancellationRequested)
                {
                    _logger.Error("Beey message pipe was unexpectedly closed, closing upload");
                    breaker.Cancel();
                }
            }
        }


    }
}
