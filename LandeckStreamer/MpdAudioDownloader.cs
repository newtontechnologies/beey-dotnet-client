using Common;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LandeckStreamer
{
    public class MpdAudioDownloader
    {
        private static readonly ILogger _logger = Log.ForContext<MpdAudioDownloader>();
        private readonly Stopwatch stopWatch = new Stopwatch();
        private readonly ConcurrentQueue<string> audio = new ConcurrentQueue<string>();


        private readonly string UrlBase = "";
        public readonly string FilePath = null;
        private readonly string MimeType = null;
        public MpdAudioDownloader(string urlbase, string filePath, string mime = "audio/mp4")
        {
            UrlBase = urlbase + "$StartDateTime$/$DownloadLength$/offline.ism/";
            FilePath = filePath;
            using var f = File.Create(filePath);
            MimeType = mime;
            stopWatch.Start();
        }

        private readonly List<string> savedSegments = new List<string>();


        volatile bool ListIsComplete = false;
        public bool DataIsComplete { get => ListIsComplete; }

        DateTime lastreload = DateTime.MinValue;

        volatile bool loading = false;

        //check if stream exists in given time
        public static (bool, XDocument) CheckStream(string urlBase, DateTime from, TimeSpan length, bool checkToEnd)
        {
            try
            {
                var m = DownloadManifest(urlBase + "$StartDateTime$/$DownloadLength$/offline.ism/", from, length);
                if (checkToEnd && !m.listComplete)
                    return (false, m.doc);
                else
                    return (true, m.doc);
            }
            catch
            {
                return (false, null);
            }
        }


        public TimeSpan ManifestLoadTotal { get; private set; } = new TimeSpan();
        public int ManifestLoadCount { get; private set; } = 0;
        public int ManifestSameRepeats { get; private set; } = 0;
        private void LoadManifest()
        {
            var time = stopWatch.Elapsed;

            if (ListIsComplete || loading)
                return;
            try
            {
                loading = true;
                lastreload = DateTime.Now;

                string initurl;
                string[] segments;
                (_, TimeScale, TimeOffset, segments, initurl, ListIsComplete) = DownloadManifest(UrlBase, From, Length, MimeType);

                if (savedSegments.Count <= 0) //header was not yet loaded
                    audio.Enqueue(initurl);

                if (segments.Count() <= savedSegments.Count) //something is wrong no new data...
                {
                    ManifestSameRepeats++;

                    if (ManifestSameRepeats > (5 + Length.TotalMinutes))
                        throw new InvalidOperationException("stuck reloading same list...");
                }
                else
                    ManifestSameRepeats = 0;


                foreach (var s in segments.Skip(savedSegments.Count))
                {
                    savedSegments.Add(s);
                    audio.Enqueue(s);
                }

            }
            finally
            {
                loading = false;
                ManifestLoadCount++;
                ManifestLoadTotal += stopWatch.Elapsed - time;
            }

        }

        private static (XDocument doc, long Timescale, long TimeOffset, string[] segments, string initURL, bool listComplete) DownloadManifest(string UrlBase, DateTime From, TimeSpan Length, string MimeType = "audio/mp4")
        {
            long timescale = 10000000;
            long timeOffset = 0;


            var url = UrlBase
                        .Replace("$StartDateTime$", From.ToString("s"))
                        .Replace("$DownloadLength$", Length.Ticks.ToString());

            XDocument doc = XDocument.Load(url + "manifest.mpd");

            var audioSet = doc.Descendants("AdaptationSet")
                .Where(e => e.Attribute("mimeType")?.Value == MimeType)
                .FirstOrDefault();

            //var lang = audioSet.Attribute("lang").Value;

            var audioSegmentTemplate = audioSet.Element("SegmentTemplate");

            var initurl = url + audioSegmentTemplate.Attribute("initialization").Value;


            var media = audioSegmentTemplate.Attribute("media").Value;

            try
            {
                timescale = long.Parse(audioSegmentTemplate.Attribute("timescale").Value);
                var offset = Regex.Match(media, @".*/(\d+)/.*$")?.Groups[1]?.Value;
                timeOffset = long.Parse(offset);
            }
            catch { }


            var audioRepresentation = audioSet.Element("Representation");
            var reprId = audioRepresentation.Attribute("id").Value;
            media = media.Replace("$RepresentationID$", reprId);

            var segments = audioSegmentTemplate.Descendants("S");
            var savedSegments = segments.Select(s => url + media.Replace("$Time$", s.Attribute("t").Value)).ToArray();

            var tickspersec = double.Parse(audioSegmentTemplate.Attribute("timescale").Value);
            timescale = (long)tickspersec;
            var virtualFromStart = DateTime.Now - From;
            segments = segments.Where(s =>
            {
                double segmentStart = long.Parse(s.Attribute("t").Value);
                var start = TimeSpan.FromSeconds(segmentStart /= tickspersec);
                return start < virtualFromStart;
            });



            var ls = segments.Last();
            double lastEnd = long.Parse(ls.Attribute("t").Value) + long.Parse(ls.Attribute("d").Value);
            lastEnd /= tickspersec;

            Console.WriteLine($"DLM - loaded segments urls for {lastEnd}s");
            return (doc, timescale, timeOffset, savedSegments, initurl, TimeSpan.FromSeconds(lastEnd + 5) > Length);
        }

        volatile bool done = false;
        DateTime From;
        TimeSpan Length;

        public long TimeOffset { get; private set; } = 0;
        public long TimeScale { get; private set; } = 10000000;

        public double TimeOffsetSeconds => (double)TimeOffset / TimeScale;


        public int DownloadedBytes { get; private set; } = 0;

        public TimeSpan DownloadLength { get; private set; }
        static TimeSpan ManyfestDownloadRetryDelay { get; } = TimeSpan.FromSeconds(2);
        public async Task DownloadStream(DateTime from, TimeSpan length, Stream writeTo, CancellationToken token = default)
        {
            var DownloadStarted = stopWatch.Elapsed;
            Console.WriteLine($"DLM - downloading from URL {UrlBase}");
            Console.WriteLine($"DLM - downloading from time {from} for {length}");
            Console.WriteLine($"-----------");
            From = from;
            Length = length;
            LoadManifest();

            try
            {
                using WebClient client = new WebClient();
                using FileStream fs = File.Open(FilePath, FileMode.Open, FileAccess.Write, FileShare.Read);

                if (token != default)
                    token.Register(() => client.CancelAsync());

                while (!done && !token.IsCancellationRequested)
                {
                    if (lastreload + TimeSpan.FromSeconds(20) < DateTime.Now)
                        LoadManifest();

                    if (audio.TryDequeue(out var url))
                    {
                        var retrycount = 3;
                        for (int i = 0; i <= retrycount; i++)
                        {
                            try
                            {
                                var data = client.DownloadData(new Uri(url));
                                var t1 = writeTo?.WriteAsync(data, 0, data.Length, token);
                                var t2 = fs.WriteAsync(data, 0, data.Length, token);
                                await Task.WhenAll(new[] { t1, t2 }.Where(t => t != null));

                                DownloadedBytes += data.Length;
                                Console.WriteLine($"DLM - W{DownloadedBytes}B - {url.Substring(UrlBase.Length)}");
                                break;
                            }
                            catch (Exception e)
                            {
                                if (i == retrycount)
                                    throw;

                                _logger.Warning(e, "Landeck downloader problem, retry in 20s");
                                await Task.Delay(20000);
                            }
                        }
                    }
                    else if (ListIsComplete)
                    {
                        writeTo?.Flush(); //wait until everything is send
                        done = true;
                        return;
                    }
                    else
                    {
                        await Task.Delay(ManyfestDownloadRetryDelay);
                        Console.WriteLine($"DLM - waiting for landeck to record new data");
                    }
                }

            }
            finally
            {
                DownloadLength = stopWatch.Elapsed - DownloadStarted;
                stopWatch.Stop();
            }
        }
    }

}
