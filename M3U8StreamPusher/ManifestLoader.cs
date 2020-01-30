using M3U8Parser;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace M3U8StreamPusher
{
    public class ManifestLoader
    {
        static readonly ILogger _logger = Log.ForContext<ManifestLoader>();
        public static readonly DateTime Epoch = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        HashSet<string> processed = new HashSet<string>();
        DateTime lastnew = DateTime.Now;
        int waitcnt = 0;
        public async IAsyncEnumerable<TrackData> DownloadTracks(string dataurl, TimeSpan? length)
        {
            if (length is null)
                length = TimeSpan.MaxValue;
            _logger.Information("manifest: {url}", dataurl);

            while (true)
            {

                Stream data = null;
                try
                {
                    HttpClient downloader = new HttpClient();
                    data = await downloader.GetStreamAsync(dataurl);
                }
                catch (Exception e)
                {
                    _logger.Fatal(e, "Manifest download failed, waiting 30s to retry");
                    await Task.Delay(TimeSpan.FromSeconds(30));
                    continue;
                }

                var parser = new PlaylistParser(data, Format.EXT_M3U, M3U8Parser.Encoding.UTF_8, ParsingMode.LENIENT);
                Playlist playlist = parser.parse();


                if (playlist.hasMasterPlaylist() || !playlist.hasMediaPlaylist() || playlist.getMediaPlaylist().getUnknownTags().Count != 1 || playlist.getMediaPlaylist().getUnknownTags().First() != $"#EXT-X-INDEPENDENT-SEGMENTS")
                {
                    Log.Fatal("unsupported media format - It needs to be a media plalist with independent segments");
                    yield break;
                }


                var media = playlist.getMediaPlaylist();
                var tracks = media.getTracks();

                bool anynew = false;
                foreach (var t in tracks)
                {
                    var uri = t.getUri();
                    if (!processed.Contains(uri))
                    {
                        processed.Add(uri);
                        yield return t;
                        anynew = true;
                    }
                }


                var reallength = TimeSpan.FromSeconds(tracks.Select(t => t.getTrackInfo().duration).Sum());
                bool haveeverything = reallength + TimeSpan.FromSeconds(0.5) >= length;

                if (haveeverything)
                {
                    _logger.Information("Manifest downloaded sucessfully with {lengt} of media file", reallength);
                    yield break;
                }

                if (waitcnt > 10)
                {
                    _logger.Warning("maximum wait time reached, manifest is completed");
                    yield break;
                }

                if (!anynew)
                {
                    _logger.Information("Manifest on server was not updated for {delay}, manifest have {lengt} of {requested}", TimeSpan.FromMilliseconds(waitcnt * (waitcnt + 1) / 2), reallength, length);
                    waitcnt++;
                    var wait = TimeSpan.FromSeconds(waitcnt);
                    _logger.Information("Waiting {wait}", wait);
                    await Task.Delay(wait);
                }
                else
                {
                    waitcnt = 0;
                }
            }
        }
    }
}
