using Common;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace LandeckStreamer
{
    public class LandeckApi
    {
        static readonly ILogger _logger = Log.ForContext<LandeckApi>();
        public static JObject LoadLandeckInfo(CommandLineOptions opts, DateTime start, TimeSpan length, ref string urlbase, ref bool IsVideo) {
            using WebClient wc = new WebClient();

            string data = wc.DownloadString(Program.Configuration.LandeckAPIURL + "search/channel/" + System.Web.HttpUtility.UrlEncode(opts.Channel));
            JObject landecks = JObject.Parse(data);

            //Checks if the channel exists
            if ((string)landecks?["status"] != "FOUND") {
                _logger.Error($"landeck did not find a stream called '{opts.Channel}', \r\n {landecks} ");
                Log.CloseAndFlush();
                return null;
            }

            var jdata = landecks?["data"]
                .OrderBy(c => c["landeck_id"])
                .Where(d => ((string)d["channel_name"])?.ToLower() == opts.Channel.ToLower());

            jdata = jdata.OrderBy(j => (string)j["landeck_id"]);
            if (!jdata.Any()) {
                _logger.Error($"landeck did not find a stream called '{opts.Channel}', \r\n {landecks} ");
                Log.CloseAndFlush();
                return null;
            }

            JObject channelInfo = null;
            foreach (JObject ci in jdata) {
                string landeckUrl = (string)ci["mss_basic_url"];
                urlbase = landeckUrl.Replace("/api2/", "/smoothconv/api2/");//Replace("ntvr.newtonmedia", "czprgntvr.newtonmedia")

                bool completed = DateTime.Now > start + length + TimeSpan.FromMinutes(2);

                var (check, manifest) = MpdAudioDownloader.CheckStream(urlbase, start, length, completed);
                if (!check)
                    continue;

                var streams = manifest.Descendants("AdaptationSet");
                IsVideo = streams.Any(s => s.Attribute("mimeType")?.Value == "video/mp4");

                channelInfo = ci;
                break;
            }

            if (channelInfo == null) {
                _logger.Error($"did not find uninterrupted stream'{opts.Channel}' on \r\n {landecks} ");
                Log.CloseAndFlush();
                return null;
            } else
                _logger.Information("Landeck & channel selected: {$landeck}", channelInfo);

            return channelInfo;
        }
    }
}
