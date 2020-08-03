using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO.Pipelines;

namespace Nanogrid.Utils
{
    /// <summary>
    /// Buffer between something that writes into stream and other thing that reads from stream
    /// Reads and writes can be done on concurrentlty (on different threads)
    /// </summary>
    public class BufferingStream : Stream
    {
        public object Tag;
        private readonly Pipe _pipeline;
        private readonly string _indumpfilename;
        private readonly string _outdumpfilename;
        private readonly PipeWriter _writer;
        readonly Stream filelogwrite;
        private readonly PipeReader _reader;
        readonly Stream filelogread;

        private long _written = 0;
        private long _read = 0;

        /// <summary>
        /// number of bytes written into BufferingStream
        /// </summary>
        public override long Length => Interlocked.Read(ref _written);

        /// <summary>
        /// get the number of bytes read from BufferingStream
        /// </summary>
        public override long Position
        {
            get => Interlocked.Read(ref _read);
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="byteCapacity">capacity of internal buffers, writes will pause on await when this much data is buffered</param>
        /// <param name="minBytesBuffered">read awaits are resumed when this much data is buffered</param>
        public BufferingStream(int byteCapacity = 32768, int minBytesBuffered = 4096, string outdumpfilename = null, string indumpfilename = null)
        {
            _pipeline = new Pipe(new PipeOptions(pauseWriterThreshold: byteCapacity, resumeWriterThreshold: minBytesBuffered));
            _indumpfilename = indumpfilename;
            _outdumpfilename = outdumpfilename;

            _writer = _pipeline.Writer;
            if (_indumpfilename != null)
                filelogwrite = File.Create(Path.GetRandomFileName() + "_" + _indumpfilename);

            _reader = _pipeline.Reader;
            if (_outdumpfilename != null)
                filelogread = File.Create(Path.GetRandomFileName() + "_" + _outdumpfilename);
        }


        #region write
        public override bool CanWrite => true && !Disposed;
        public override void Write(byte[] buffer, int offset, int count) => WriteAsync(buffer, offset, count).Wait();

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var res = await _writer.WriteAsync(buffer, cancellationToken);
            if (filelogwrite != null)
                await filelogwrite.WriteAsync(buffer);

            Interlocked.Add(ref _written, buffer.Length);
        }

        TaskCompletionSource<bool> completionWait = new TaskCompletionSource<bool>();
        public async Task WaitForCompletionAsync(CancellationToken cancellationToken = default)
        {
            await completionWait.Task;
        }

        /// <summary>
        /// Indicate that all data was written
        /// </summary>
        public void CompleteWrite()
        {
            _pipeline.Writer.Complete();
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => await WriteAsync(buffer.AsMemory(offset, count), cancellationToken);

        public override async Task FlushAsync(CancellationToken cancellationToken) => await _writer.FlushAsync();
        public override void Flush() => FlushAsync().Wait();

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            Task t = null;

            t = WriteAsync(buffer, offset, count)
                .ContinueWith((tsk, s) =>
                {
                    callback?.Invoke(t);
                }, state);
            return t;
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            var res = asyncResult as Task;
            if (res is null)
                throw new InvalidOperationException("asyncResult is certainly not from ReginRead");


            res.Wait();
        }

        #endregion
        #region read
        public override bool CanRead => true && !Disposed;
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = 0;
            var r = await _reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (r.IsCompleted && r.Buffer.Length == 0)
            {
                completionWait.SetResult(true);
                return 0;
            }

            foreach (var bfr in r.Buffer)
            {
                if (read + bfr.Length <= buffer.Length)
                {
                    bfr.CopyTo(buffer.Slice(read, bfr.Length));
                    if (filelogread != null)
                        await filelogread.WriteAsync(bfr);
                    read += bfr.Length;
                }
                else
                {
                    int toread = buffer.Length - read;
                    var wslice = bfr.Slice(0, toread);
                    wslice.CopyTo(buffer.Slice(read, toread));
                    if (filelogread != null)
                        await filelogread.WriteAsync(wslice);
                    read += toread;
                }
            }
            Interlocked.Add(ref _read, read);
            _reader.AdvanceTo(r.Buffer.GetPosition(read));
            return read;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            Task<int> t = null;

            t = ReadAsync(buffer, offset, count)
                .ContinueWith((tsk, s) =>
                {
                    callback?.Invoke(t);
                    return tsk.Result;
                }, state);

            return t;
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            var res = asyncResult as Task<int>;
            if (res is null)
                throw new InvalidOperationException("asyncResult is certainly not from ReginRead");


            return res.Result;
        }

        public override int Read(byte[] buffer, int offset, int count)
            => ReadAsync(buffer, offset, count).Result;

        #endregion

        public override bool CanSeek => false;

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        private bool Disposed { get; set; }
        public bool Terminated => completionWait.Task.IsCompleted;

        protected override void Dispose(bool disposing)
        {
            //Stream.Close() calls Dispose(true) - see documentation, beware of non-obvious recurrent call Close<->Dispose
            //Close should not be overriden and cleanup should be only in properly implemented in Dispose(true) call
            base.Dispose(disposing);
            if (disposing)
            {
                _pipeline?.Reader?.Complete();
                _pipeline?.Writer?.Complete();
            }
            Disposed = true;
        }
    }
}
