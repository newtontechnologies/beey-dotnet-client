using Beey.Client;
using Beey.DataExchangeModel.Projects;
using HtmlAgilityPack;
using M3U8Parser;
using Microsoft.Extensions.Configuration;
using Nanogrid.Utils;
using Newtonsoft.Json.Linq;
using Serilog;
using StreamPusher;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace M3U8StreamPusher
{
    class Program
    {
        static readonly DateTime Epoch = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        static readonly ILogger _logger = Log.ForContext<Program>();
        static DateTime? Start;
        static TimeSpan? Length;
        static TimeSpan? Skip;

        public static BeeyConfiguration Configuration { get; private set; }

        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("pusher.log")
                .CreateLogger();

            Console.WriteLine("argument 0 url with video player on sejm page 'http://www.sejm.gov.pl/Sejm9.nsf/transmisje.xsp?unid=933AA220B56F8D07C12584F8004A1ED0'");
            Console.WriteLine("argument 1 (optional) Length (Timespan) in ISO 8601 format ('2019-11-19T11:45:04+1')");
            Console.WriteLine("argument 2 (optional) Start datetime in ISO 8601 format ('02:45:15')");
            Console.WriteLine("argument 4 (optional) Skip first X minutes in manifest ISO 8601 format ('01:00:00')");

            //public static FileSourceConfiguration? FileSources => Root?.GetSection("FileSources").Get<FileSourceConfiguration>();
            //private static IChangeToken? _changeToken;
            var conf = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddXmlFile("Settings.xml", optional: false)
                .Build();

            Configuration = conf.GetSection("Beey").Get<BeeyConfiguration>();

            if (args.Length < 1 || args.Length > 4)
            {
                _logger.Error("wrong number of arguments");
                return;
            }

            var pageurl = args[0];

            _logger.Information("Parsing page {page} for media streams", pageurl);
            if (args.Length > 1)
            {
                Length = TimeSpan.Parse(args[1]);
                _logger.Information("Maximum of {length} will be transcribed", Length);
            }

            if (args.Length > 2)
            {
                Start = DateTime.Parse(args[2]);
                _logger.Information("Start time {start} will be requested as stream start", Start);
            }

            if (args.Length > 3)
            {
                Skip = TimeSpan.Parse(args[3]);
                _logger.Information("{skip} will be transcribed", Skip);
            }

            var url = await ExtractMediaUrl(pageurl, Length, Start);
            if (url is null)
                return;

            _logger.Information("starting download with start:{start}, length:{duration}", Start, Length);

            Console.CancelKeyPress += Console_CancelKeyPress;

            ManifestLoader loader = new ManifestLoader();

            var tracks = loader.DownloadTracks(url, Length, Skip, ctrlc.Token);

            var beeyurl = Configuration.URL;
            var login = Configuration.Login;
            var pass = Configuration.Password;
            _logger.Information("logging into beey");
            var beey = new BeeyClient(beeyurl);
            await beey.LoginAsync(login, pass);
            _logger.Information("logged in");

            var now = DateTime.Now;
            StreamWriter msw = null;
            if (Configuration.LogMessages)
                msw = new StreamWriter(File.Create("sejm_" + Start.Value.ToString("yyyy'-'MM'-'dd'T'HH'-'mm'-'ss") + ".msgs")) { AutoFlush = true };

            var projectname = $"sejm {Start}; {Length}";
            var p = await beey.CreateProjectAsync(new ParamsProjectInit() { Name = projectname, CustomPath = projectname });


            _logger.Information("Created project {name} {@project}", projectname, p);

            var watchdog = Listener(beey, p, msw);
            await Task.Delay(TimeSpan.FromSeconds(2));
            _logger.Information("upload started");
            await UploadTracks(tracks, beey, p);

            _logger.Information("waiting for transcription to finish");
            await watchdog;
            _logger.Information("transcription finished");

        }

        static CancellationTokenSource ctrlc = new CancellationTokenSource();
        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (ctrlc.IsCancellationRequested)
                return;
            e.Cancel= true;
            _logger.Warning("User intiatiated exit.. Upload will stop. You can kill the application with another press of Ctrl+C");
            ctrlc.Cancel();
        }

        private static async Task<string> ExtractMediaUrl(string pageurl, TimeSpan? length, DateTime? start)
        {
            HttpClient downloader = new HttpClient();
            var s = await downloader.GetStringAsync(pageurl);

            var doc = new HtmlDocument();
            doc.LoadHtml(s);
            var archid = new Uri(pageurl).Fragment?.Trim()?.Trim('#');
            var data = FindLiveStreamData(doc);
            if (data is null)
                data = FindArchivedStreamData(doc, archid);


            if (data is null)
            {
                _logger.Fatal("There are no usable streams in given URL");
                return null;
            }

            var baseurl = data["params"]["file"].Value<string>();
            var startms = DateTime.UnixEpoch.ToUniversalTime() + TimeSpan.FromMilliseconds(data["startMilis"].Value<long>());



            var dstart = data["start"].ToObject<DateTime>();

            var timezoneOffset = dstart - startms;

            _logger.Information("Stream start found {start:o}{offset}", dstart, timezoneOffset.TotalHours.ToString("+#;-#;"));
            if (!start.HasValue)
            {
                Program.Start = start = startms;
                _logger.Information("Stream start loaded from page: {start:o}", start.Value.ToLocalTime());
            }
            var dstops = data["params"]["stop"].Value<string>();

            TimeSpan totallength = TimeSpan.MaxValue;
            if (DateTime.TryParse(dstops, out var dstop))
            {
                totallength = dstop - dstart;
                _logger.Information("Stream total length found: {length}", totallength);
            }

            if (!length.HasValue && totallength != TimeSpan.MaxValue)
            {
                Program.Length = totallength;
                _logger.Information("Stream length loaded from page: {start:o}", start.Value.ToLocalTime());
            }


            baseurl = baseurl.Replace("/nvr/", "/livehls/") + "/playlist.m3u8";


            var startts = startms - Epoch;
            baseurl = $"{baseurl}?startTime={(long)(startts).TotalMilliseconds}";


            var manifest = await downloader.GetStreamAsync(baseurl);
            var parser = new PlaylistParser(manifest, Format.EXT_M3U, M3U8Parser.Encoding.UTF_8, ParsingMode.LENIENT);
            Playlist playlist = parser.parse();

            if (playlist.hasMasterPlaylist())
            {
                var mp = playlist.getMasterPlaylist();
                var streams = mp.getPlaylists();

                var stream = streams.FirstOrDefault()?.getUri();
                _logger.Information("Found {count} media streams, selecting {stream}", streams.Count, stream);

                startts = Program.Start.Value - Epoch;
                string dataurl;
                if (!stream.Contains("startTime="))
                    dataurl = $"{stream}&startTime={(long)startts.TotalMilliseconds}";
                else
                    dataurl = stream;

                if (Program.Length.HasValue && Program.Length > TimeSpan.Zero)
                    dataurl = $"{dataurl}&stopTime={(long)(startts + Program.Length.Value).TotalMilliseconds}";

                return dataurl;
            }

            return null;
        }

        private static JObject FindArchivedStreamData(HtmlDocument doc, string archid)
        {
            _logger.Information("Searching for archived stream in given URL");
            var details = doc.DocumentNode.SelectNodes("//div[@class='transDetails']");
            var datacont = details.FirstOrDefault(d => d.InnerHtml.Contains(archid));
            var datas = datacont.SelectSingleNode("span[@class='json hidden']")?.InnerText?.Trim();

            if (string.IsNullOrWhiteSpace(datas))
            {
                _logger.Information("Canot extract archive stream from given url");
                return null;
            }

            var data = JObject.Parse(datas);
            _logger.Information("archived stream information found");
            return data;
        }

        private static JObject FindLiveStreamData(HtmlDocument doc)
        {
            _logger.Information("Searching for livestream in given URL");
            var scripts = doc.DocumentNode.SelectNodes("//script");
            var pars = scripts.Where(s => s.InnerText.Contains(@"var params = {""db"":"));
            var urlscript = pars.FirstOrDefault()?.InnerText;


            if (string.IsNullOrWhiteSpace(urlscript))
            {
                _logger.Information("Cannot extract stream media script url");
                return null;
            }
            Regex jsonextractor = new Regex("{.*}");

            var m = jsonextractor.Match(urlscript);
            if (!m.Success)
            {
                _logger.Information("Cannot extract stream media data");
                return null;
            }

            var data = JObject.Parse(m.Value);
            _logger.Information("Livestream information found");
            return data;
        }

        public static async Task<long> UploadTracks(IAsyncEnumerable<TrackData> data, BeeyClient beey, Project proj)
        {
            BufferingStream bs = new BufferingStream(512 * 1024, outdumpfilename: Configuration.LogUpload ? $"_sejm_{Start:yyyy'-'MM'-'dd'T'HH'-'mm'-'ss}.ts" : null);
            var writer = WriteTracks(data, bs);
            var upload = beey.UploadStreamAsync(proj.Id, "sejm", bs, null, Configuration.TranscriptionLocale, true, breaker.Token);

            await Task.WhenAll(writer, upload);
            return bs.Length;
        }

        private static async Task WriteTracks(IAsyncEnumerable<TrackData> data, BufferingStream bs)
        {
            HttpClient downloader = new HttpClient();
            int cnt = 0;
            await foreach (var t in data)
            {
                _logger.Information("downloading segment:{cnt} {uri}", cnt, t.getUri());
                var s = await downloader.GetStreamAsync(t.getUri());
                await s.CopyToAsync(bs);
                cnt++;

                if (ctrlc.IsCancellationRequested)
                    break;
            }

            bs.CompleteWrite();
            _logger.Information("Allwriting done");
        }

        static readonly CancellationTokenSource breaker = new CancellationTokenSource();
        public static async Task Listener(BeeyClient beey, Project proj, StreamWriter writer)
        {
            try
            {
                _logger.Information("Opening websocket to listen to beey messages");
                var messages = await beey.ListenToMessages(proj.Id, breaker.Token);

                _logger.Information("Listening connected");
                await foreach (var s in messages)
                {
                    if (Configuration.MessageEcho)
                        Console.WriteLine(s);
                    if (writer != null)
                        await writer.WriteLineAsync(s);

                    if (s.Contains("RecognitionMsg") && !s.Contains("Started"))
                    {
                        _logger.Information("transcription ended on server with message {message}", s);
                        breaker.Cancel();
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Listening to beey messages failed");
            }
            breaker.Cancel();
        }
    }
}
