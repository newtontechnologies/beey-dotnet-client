using M3U8Parser;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JobScheduling
{
    public class ManifestLoader
    {
        static readonly ILogger _logger = Log.ForContext<ManifestLoader>();
        public static readonly DateTime Epoch = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        HashSet<string> processed = new HashSet<string>();
        DateTime lastnew = DateTime.Now;
        int waitcnt = 0;

        public async IAsyncEnumerable<TrackData> DownloadTracks(string dataurl, TimeSpan? length, TimeSpan? Skip = null, CancellationToken breaker = default)
        {
            if (length is null || length == TimeSpan.Zero)
                length = TimeSpan.MaxValue;
            _logger.Information("manifest: {url}", dataurl);
            var skip = Skip ?? TimeSpan.Zero;
            var skipped = 0;
            TimeSpan sent = TimeSpan.Zero;

            while (!breaker.IsCancellationRequested)
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

                if (playlist.hasMasterPlaylist() 
                    || !playlist.hasMediaPlaylist())
                {
                    Log.Fatal("unsupported media format - It needs to be a media playlist with independent segments");
                    yield break;
                }


                var media = playlist.getMediaPlaylist();
                var tracks = media.getTracks();
                bool anynew = false;

                if (skip > TimeSpan.Zero)
                    _logger.Information("Skipping first {skip} in manifest", skip);
                foreach (var t in tracks)
                {
                    if (breaker.IsCancellationRequested)
                        yield break;
                    var uri = t.getUri();
                    if (!processed.Contains(uri))
                    {
                        processed.Add(uri);
                        if (skip > TimeSpan.Zero)
                        {
                            skip -= TimeSpan.FromSeconds(t.getTrackInfo().duration);
                            skipped++;
                            if (skip < TimeSpan.Zero)
                                _logger.Information("Skipped {cnt} segments, starting transcription", skipped);
                        }
                        else
                        {
                            yield return t;
                            sent += TimeSpan.FromSeconds(t.getTrackInfo().duration);

                            if (sent >= length)
                            {
                                _logger.Information("Manifest downloaded sucessfully with {lengt} of media file", sent);
                                yield break;
                            }
                        }
                        anynew = true;
                    }
                    if (breaker.IsCancellationRequested)
                        yield break;
                }


                if (waitcnt > 10)
                {
                    _logger.Warning("maximum wait time reached, manifest is completed");
                    yield break;
                }

                if (!anynew)
                {
                    _logger.Information("Manifest on server was not updated for {delay}, manifest have {lengt} of {requested}", TimeSpan.FromSeconds(waitcnt * (waitcnt + 1) / 2), sent, length);
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
