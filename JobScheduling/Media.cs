using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JobScheduling
{
    public abstract class Media
    {
        public abstract bool SupportsRawStream { get; }
        public abstract Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token = default);

        public async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken token = default)
        {
            int read = 0;
            byte[] buffer = new byte[bufferSize];
            while ((read = await ReadAsync(buffer, 0, bufferSize, token)) > 0)
            {
                await destination.WriteAsync(buffer, 0, read, token);
            }
        }
        public Task CopyToAsync(Stream destination, CancellationToken token = default)
            => CopyToAsync(destination, 81920 /* Stream.CopyToAsync default */, token);
        public virtual Stream GetStream()
        {
            if (SupportsRawStream)
                throw new NotImplementedException();
            else
                throw new NotSupportedException();
        }
    }
}
