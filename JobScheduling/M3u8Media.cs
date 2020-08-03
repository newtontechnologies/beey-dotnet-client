using M3U8Parser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace JobScheduling
{
    class M3u8Media : Media
    {
        private Uri mediaPlaylistUrl;
        private TimeSpan? duration;
        private TimeSpan? skip;
        private readonly string baseUrl;
        private readonly HttpClient client;
        private (IAsyncEnumerator<TrackData> Enumerator, Stream Stream)? data;

        public override bool SupportsRawStream => false;

        public M3u8Media(Uri mediaPlaylistUrl, TimeSpan? duration, TimeSpan? skip)
        {
            this.mediaPlaylistUrl = mediaPlaylistUrl;
            this.duration = duration;
            this.skip = skip;

            baseUrl = mediaPlaylistUrl.AbsoluteUri[..^mediaPlaylistUrl.Segments.Last().Length];
            client = new HttpClient();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token = default)
        {
            if (data == null)
            {
                // open-m3u8 fails when parsing floating point numbers without setting culture
                var originalCulture = CultureInfo.DefaultThreadCurrentCulture;
                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
                try
                {
                    var manifestLoader = new ManifestLoader();
                    var dataEnumerator = manifestLoader.DownloadTracks(mediaPlaylistUrl.AbsoluteUri, duration, skip, token).GetAsyncEnumerator(token);
                    await dataEnumerator.MoveNextAsync();
                    var stream = await client.GetStreamAsync(GetUri(dataEnumerator.Current));
                    data = (dataEnumerator, stream);
                }
                finally
                {
                    CultureInfo.DefaultThreadCurrentCulture = originalCulture;
                }
            }

            int read = await data.Value.Stream.ReadAsync(buffer, offset, count, token);
            while (read < count)
            {
                if (!await data.Value.Enumerator.MoveNextAsync())
                    return read;

                data = (data.Value.Enumerator, await client.GetStreamAsync(GetUri(data.Value.Enumerator.Current)));

                read += await data.Value.Stream.ReadAsync(buffer, offset + read, count - read, token);
            }

            return read;
        }

        private string GetUri(TrackData current)
        {
            var uri = current.getUri();
            if (!uri.StartsWith("http"))
                uri = baseUrl + uri;

            return uri;
        }
    }
}