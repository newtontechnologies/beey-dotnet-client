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
        static readonly CancellationTokenSource breaker = new CancellationTokenSource();

        static readonly DateTime ProgramStarted = DateTime.UtcNow;

        static void Main(string[] args) {
            //Start logging
            Log.Logger = new LoggerConfiguration()
               .Enrich.With(new ProcessIdEnricher())
               .WriteTo.File("LandeckStreamer.log", rollingInterval: RollingInterval.Day, shared: true, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {ProcessId} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
               .WriteTo.Console()
               .CreateLogger();

            _logger = Log.ForContext<Program>();
            _logger.Information("Starting with Args {@Args}", string.Join(" ", args));

            //Load settings
            var conf = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddXmlFile("Settings.xml", optional: false)
                .Build();

            //Beey server settings + credentials
            Configuration = conf.GetSection("Beey").Get<BeeyConfiguration>();

            //Get command line arguments and starts
            Parser.Default.ParseArguments<CommandLineOptions>(args)
            .WithParsed(opts =>
            {
                try {
                    MainAsync(opts).Wait();
                }
                catch (AggregateException ae) when (ae.InnerException is InvalidOperationException iox && iox.Message == "stuck reloading same list...") {
                    _logger.Fatal(iox, "Failed with unrecoverable Landeck download error");
                    Log.CloseAndFlush();
                    return;
                }
                catch (Exception e) {
                    _logger.Fatal(e, "Failed with unrecoverable error");
                    Log.CloseAndFlush();
                    return;
                }
            });
            _logger.Information("Terminating successfully");
        }

        //Most stuff happens here
        private static async Task MainAsync(CommandLineOptions opts) {
            string projectname = opts.Title;
            string urlbase = null;
            bool isVideo = false;

            //Checks if the channel is valid + gets information about the channel
            JObject channelInfo = LandeckApi.LoadLandeckInfo(opts, opts.Start, opts.Length, ref urlbase, ref isVideo);

            int count = 5;
            while (channelInfo is null || (channelInfo.TryGetValue("stream_count", out var ssc) && ssc.Value<string>() == "2" && isVideo == false)) {
                _logger.Warning("Stream count mismatch when loading from landeck. Retry in 1s");
                count--;
                await Task.Delay(1000);
                channelInfo = LandeckApi.LoadLandeckInfo(opts, opts.Start, opts.Length, ref urlbase, ref isVideo);

                if(count <= 0) {
                    _logger.Fatal("Failed to load landeck information");
                    return;
                }
            }

            //Download the video/audio streams
            var videodownloader = isVideo ? new MpdAudioDownloader(urlbase, opts.Title + Path.GetRandomFileName() + "video.mp4", "video/mp4") : null;
            var audiodownloader = new MpdAudioDownloader(urlbase, opts.Title + Path.GetRandomFileName() + "audio.mp4", "audio/mp4");

            Task videoDownloadTask = isVideo ? videodownloader.DownloadStream(opts.Start, opts.Length, null) : null;
            Task audioDownloadTask = audiodownloader.DownloadStream(opts.Start, opts.Length, null);

            Process ffmpeg;
            if (isVideo) {
                ffmpeg = Process.Start(new ProcessStartInfo()
                {
                    FileName = Configuration.FFmpeg,
                    Arguments = string.Format(Configuration.FFmpegVideoParams, audiodownloader.FilePath, videodownloader.FilePath),
                    RedirectStandardOutput = true,
                });
            } else {
                ffmpeg = Process.Start(new ProcessStartInfo()
                {
                    FileName = Configuration.FFmpeg,
                    Arguments = string.Format(Configuration.FFmpegAudioParams, audiodownloader.FilePath),
                    RedirectStandardOutput = true,
                });
            }

            //Beey transcription process
            string beeyurl = Configuration.URL;
            string login = Configuration.Login;
            string pass = Configuration.Password;

            BeeyClient beey = new BeeyClient(beeyurl);

            _logger.Information("Logging into beey");
            await beey.LoginAsync(login, pass);
            _logger.Information("Logged in");

            StreamWriter msw = null;
            if (Configuration.LogMessages)
                msw = new StreamWriter(File.Create($"pusher{ProgramStarted:yyyy'-'MM'-'dd'T'HH'-'mm'-'ss}.msgs")) { AutoFlush = true };

            Project project = await beey.CreateProjectAsync(new ParamsProjectInit() { Name = projectname, CustomPath = projectname });
            _logger.Information("Created project {name} {@project}", projectname, project);

            Task watchdog = Listener(beey, project, msw);
            await Task.Delay(2000);

            _logger.Information("Upload started");
            Task upload = beey.UploadStreamAsync(project.Id, "sejm", ffmpeg.StandardOutput.BaseStream, null, true, breaker.Token);

            bool repeat = true;
            while (repeat) {
                try {
                    project = await beey.TranscribeProjectAsync(project.Id, Configuration.TranscriptionLocale, cancellationToken: breaker.Token);
                    repeat = false;
                }
                catch (Exception) {
                    _logger.Warning("Not yet ready to start transcription");
                }
            }
            await upload;

            _logger.Information("Transcription finished");
            await Task.WhenAll(audioDownloadTask, videoDownloadTask, upload, watchdog);
        }

        //Websocket communication for beey messages
        public static async Task Listener(BeeyClient beey, Project proj, StreamWriter writer) {
            bool finished = false;
            try {
                if (writer != null) {
                    await writer.WriteAsync('[');
                    await writer.WriteLineAsync();
                }
                _logger.Information("Opening websocket to listen to beey messages");
                var messages = await beey.ListenToMessages(proj.Id, breaker.Token);

                _logger.Information("Listening connected");
                await foreach (var s in messages) {
                    if (Configuration.MessageEcho)
                        Console.WriteLine(s);
                    if (writer != null) {
                        await writer.WriteAsync(s);
                        await writer.WriteAsync(',');
                        await writer.WriteLineAsync();
                    }

                    if (s.Contains("Failed")) {
                        _logger.Error("Server reported processing error: {message}", s);
                    }

                    //Ends beey messaging task
                    if (s.Contains("TranscriptionTracking") && s.Contains("Completed")) {
                        _logger.Information("Transcription ended on server with message {message}", s);
                        finished = true;
                        breaker.Cancel();
                    }
                }
            }
            catch (Exception e) {
                if (e is TaskCanceledException && !finished)
                    _logger.Error(e, "Listening to beey messages failed");
            }
            finally {
                if (writer != null) {
                    await writer.WriteLineAsync();
                    await writer.WriteAsync(']');
                    writer.Close();
                }

                if (!breaker.IsCancellationRequested) {
                    _logger.Error("Beey message pipe was unexpectedly closed, closing upload");
                    breaker.Cancel();
                }
            }
        }
    }
}
