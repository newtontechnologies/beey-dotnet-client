using Beey.Client;
using Beey.DataExchangeModel.Projects;
using M3U8Parser;
using Nanogrid.Utils;
using Serilog;
using StreamPusher;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace M3U8StreamPusher
{
    class Program
    {
        static readonly ILogger _logger = Log.ForContext<Program>();
        static DateTime start;
        static TimeSpan length;
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("pusher.log")
                .CreateLogger();

            Console.WriteLine("arguments: Start datetime in ISO 8601 format, Length (Timespan) in ISO 8601 format ('2019-11-19T11:45:04+1 01:15:36')");

            if (args.Length != 2)
            {
                _logger.Error("wrong number of arguments");
                return;
            }


            start = DateTime.Parse(args[0]);
            length = TimeSpan.Parse(args[1]);
            _logger.Information("starting download with start:{start}, length:{duration}", start, length);

            //var t1 = TimeSpan.FromMilliseconds(595853104017);
            //var t2 = TimeSpan.FromMilliseconds(595861240000);            
            //var dataurl = @"http://r.dcs.redcdn.pl/livehls/o2/sejm/ENC27/live.livx/playlist.m3u8?bitrate=1028000&audioId=1&videoId=4&startTime=595853104017&stopTime=595861240000";

            ManifestLoader loader = new ManifestLoader();

            var tracks = loader.DownloadTracks(start, length);

            var beeyurl = @"http://localhost:61497";
            var login = @"ladislav.seps@newtontech.cz";
            var pass = "OVPgod";
            _logger.Information("logging into beey");
            var beey = new BeeyClient(beeyurl);
            await beey.LoginAsync(login, pass);
            _logger.Information("logged in");

            var now = DateTime.Now;
            using var msw = new StreamWriter(File.Create("sejm_" + start.ToString("yyyy'-'MM'-'dd'T'HH'-'mm'-'ss") + ".msgs"));

            var projectname = $"sejm {start}; {length}";
            var p = await beey.CreateProjectAsync(new ParamsProjectInit() { Name = projectname, CustomPath = projectname });


            _logger.Information("Created project {name} {@project}", projectname, p);

            var watchdog = Listener(beey, p, msw);
            _logger.Information("upload started");
            await UploadTracks(tracks, beey, p);

            _logger.Information("waiting for transcription to finish");
            await watchdog;
            _logger.Information("transcription finished");

        }

        public static async Task<long> UploadTracks(IAsyncEnumerable<TrackData> data, BeeyClient beey, Project proj)
        {

            BufferingStream bs = new BufferingStream(512 * 1024, outdumpfilename: $"_sejm_{start:yyyy'-'MM'-'dd'T'HH'-'mm'-'ss}.ts");
            var writer = WriteTracks(data, bs);
            var upload = beey.UploadStreamAsync(proj.Id, "sejm", bs, null, "pl-PL", true, breaker.Token);

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
            }

            bs.CompleteWrite();

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

                    if (s.Contains("RecognitionMsg") && !s.Contains("Started"))
                    {
                        _logger.Information("transcription ended on server with message {message}", s);
                        breaker.Cancel();
                    }
                }
            }
            catch
            {

            }
            breaker.Cancel();
        }
    }
}
