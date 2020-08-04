using M3U8Parser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JobScheduling
{
    class M3u8MediaSource : MediaSource<M3u8Media>
    {
        public M3u8MediaSource(Uri uri) : base(uri)
        {
        }


        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override async Task<M3u8Media> LoadMediaAsync(TimeSpan? duration, TimeSpan? skip, CancellationToken cancellationToken = default)
        {
            string baseUrl = uri.AbsoluteUri[..^uri.Segments.Last().Length];
            using (var client = new HttpClient())
            using (var manifest = await client.GetStreamAsync(uri.AbsoluteUri))
            {
                var parser = new PlaylistParser(manifest, Format.EXT_M3U, M3U8Parser.Encoding.UTF_8, ParsingMode.LENIENT);
                var playlist = parser.parse();
                string? mediaPlaylistUrl = uri.AbsoluteUri;
                if (playlist.hasMasterPlaylist())
                {
                    var masterPlaylist = playlist.getMasterPlaylist();
                    mediaPlaylistUrl = masterPlaylist.getPlaylists().FirstOrDefault()?.getUri();
                }

                if (mediaPlaylistUrl == null)
                    throw new ArgumentException("Stream not found.");
                if (!mediaPlaylistUrl.StartsWith("http"))
                    mediaPlaylistUrl = baseUrl + mediaPlaylistUrl;

                return new M3u8Media(new Uri(mediaPlaylistUrl), duration, skip);
            }
        }
    }
}
