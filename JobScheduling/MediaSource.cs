using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JobScheduling
{
    public abstract class MediaSource<TMedia> : IDisposable
        where TMedia : Media
    {
        protected readonly Uri uri;

        public MediaSource(Uri uri)
        {
            this.uri = uri;
        }

        public abstract void Dispose();
        public Task<TMedia> LoadMediaAsync(TimeSpan? duration, CancellationToken cancellationToken = default)
            => LoadMediaAsync(duration, null, cancellationToken);
        public Task<TMedia> LoadMediaAsync(CancellationToken cancellationToken = default)
            => LoadMediaAsync(null, null, cancellationToken);
        public abstract Task<TMedia> LoadMediaAsync(TimeSpan? duration, TimeSpan? skip, CancellationToken cancellationToken = default);
    }
}
