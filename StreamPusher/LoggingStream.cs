using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StreamPusher
{
    class LoggingStream : Stream
    {
        public Stream LoggedStream { get; }
        public long TotalRead { get; internal set; }
        public LoggingStream(Stream loggedStream)
        {
            LoggedStream = loggedStream;
        }

        public override bool CanRead => LoggedStream.CanRead;

        public override bool CanSeek => LoggedStream.CanSeek;

        public override bool CanWrite => LoggedStream.CanWrite;

        public override long Length => LoggedStream.Length;

        public override long Position { get => LoggedStream.Position; set => LoggedStream.Position = value; }


        public override void Flush() => LoggedStream.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => LoggedStream.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count)
        {
            return LoggedStream.Read(buffer, offset, count);
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return LoggedStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return LoggedStream.ReadAsync(buffer, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin) => LoggedStream.Seek(offset, origin);

        public override void SetLength(long value) => LoggedStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => LoggedStream.Write(buffer, offset, count);
    }
}
