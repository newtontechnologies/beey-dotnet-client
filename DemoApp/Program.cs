using Beey.Api.Rest;
using Beey.Client;
using Beey.DataExchangeModel.Messaging;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using TranscriptionCore;

namespace DemoApp
{
    abstract class MediaSource : IDisposable
    {
        protected readonly Uri uri;

        public abstract bool SupportsRawStream { get; }

        public MediaSource(Uri uri)
        {
            this.uri = uri;
        }

        public abstract void Dispose();
        public abstract Task<int> ReadAsync(byte[] buffer, int count, CancellationToken token);
        public abstract Stream GetRawStream();
    }

    class LocalFileMediaSource : MediaSource
    {
        private bool disposedValue;
        private FileStream? stream;

        public override bool SupportsRawStream => true;

        public LocalFileMediaSource(Uri uri) : base(uri)
        {
            disposedValue = false;
            stream = null;
        }

        public override Task<int> ReadAsync(byte[] buffer, int count, CancellationToken token)
        {
            stream ??= File.OpenRead(uri.ToString());
            return stream.ReadAsync(buffer, 0, count, token);
        }

        public override Stream GetRawStream()
        {
            stream ??= File.OpenRead(uri.ToString());
            return stream;
        }

        #region IDisposable

        protected void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    stream?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MediaSource()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public override void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable
    }

    abstract class Media
    {
        private readonly MediaSource source;
        private readonly TimeSpan? skip;
        private readonly TimeSpan? duration;
        private TimeSpan timeRead;

        private bool skipped = false;

        public Media(MediaSource source) : this(source, null, null) { }
        public Media(MediaSource source, TimeSpan? skip, TimeSpan? duration)
        {
            this.source = source;
            this.skip = skip;
            this.duration = duration;
            timeRead = TimeSpan.Zero;
        }

        protected abstract Task<(int Read, TimeSpan TimeRead)> ReadWithTimeAsync(byte[] buffer, int count, CancellationToken token);
        public async Task<int> ReadAsync(byte[] buffer, int count, CancellationToken token = default)
        {
            if (duration.HasValue && timeRead >= duration.Value)
            {
                return 0;
            }

            if (!skipped)
            {
                if (skip.HasValue)
                {
                    TimeSpan skipTime = TimeSpan.Zero;
                    int skipRead = 0;
                    do
                    {
                        var res = await ReadWithTimeAsync(buffer, count, token);
                        skipTime += res.TimeRead;
                        skipRead = res.Read;
                    } while (skipRead > 0 && skipTime < skip.Value);
                }
                skipped = true;
            }

            var (read, time) = await ReadWithTimeAsync(buffer, count, token);
            timeRead += time;
            if (duration.HasValue && timeRead > duration.Value)
                return 0;
            else
                return read;
        }

        public async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken token = default)
        {
            int read = 0;
            byte[] buffer = new byte[bufferSize];
            while ((read = await ReadAsync(buffer, bufferSize, token)) > 0)
            {
                await destination.WriteAsync(buffer, 0, read, token);
            }
        }
        public Task CopyToAsync(Stream destination, CancellationToken token = default)
            => CopyToAsync(destination, 81920 /* Stream.CopyToAsync default */, token);
    }

    class Mp3Media : Media
    {
        public Mp3Media(MediaSource source) : base(source)
        {
        }

        public Mp3Media(MediaSource source, TimeSpan? skip, TimeSpan? duration) : base(source, skip, duration)
        {
        }

        protected override Task<(int Read, TimeSpan TimeRead)> ReadWithTimeAsync(byte[] buffer, int count, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }

    class Program
    {
        private static IConfiguration config;

        private static async Task Test()
        {
            var uri = new Uri("url");
            MediaSource mediaSource = new LocalFileMediaSource(uri);
            Media media = new Mp3Media(mediaSource, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(25));
            using (var stream = new MemoryStream())
            {
                Task reader = media.CopyToAsync(stream);
                Task writer = null; // upload stream to beey
                await reader;
                await writer;
            }



        }

        static async Task Main(string[] args)
        {
            var a = AppDomain.CurrentDomain.BaseDirectory;
            a = Path.Combine(a, "..", "..", "..");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("beey.log")
                .CreateLogger();

            //command line arguments: DemoApp.exe audio.mp4
            if (args.Length < 1)
            {
                Console.WriteLine("Please specify the file to be transcribed in the following format:\nthis.exe audio.mp4.");
                return;
            }
            else if (!File.Exists(args[0]))
            {
                Console.WriteLine("Audio file not found. Please make sure that the file exists.");
                return;
            }
            else if (!File.Exists("Settings.xml"))
            {
                Console.WriteLine("Settings.xml file not found. Please make sure that this file exists.");
                return;
            }

            //load settings
            string audioPath = args[0];
            string url = "";
            string email = "";
            string password = "";

            config = new ConfigurationBuilder()
                  .SetBasePath(Directory.GetCurrentDirectory())
                  .AddXmlFile("Settings.xml", optional: false, reloadOnChange: true)
                  .Build();

            url = config.GetValue<String>("Beey-Server:Url");
            email = config.GetValue<String>("Credentials:Email");
            password = config.GetValue<String>("Credentials:Password");

            var beey = new BeeyClient(url); //api

            ////login
            Console.WriteLine("Logging in.");
            await beey.LoginAsync(email, password);

            //create project
            Console.WriteLine("Creating project.");
            var project = await beey.CreateProjectAsync("projectname", "projectpath", default);

            //upload file
            Console.WriteLine("Uploading file.");
            var fileStream = File.Open(audioPath, FileMode.Open);
            await beey.UploadStreamAsync(project.Id, "audioname.mp3", fileStream, fileStream.Length, false, default);

            //wait for transcoding
            Console.WriteLine("Waiting for transcoding to finish, this may take a while.");
            int retryCount = 5;
            TryValueResult<ProjectProgress> result;
            //periodically checks if the server has finished a given process until true
            while ((result = await beey.GetProjectProgressStateAsync(project.Id, default).TryAsync())
                && !ProcessState.Finished.HasFlag(result.Value.TranscodingState)
                && retryCount > 0)
            {
                await Task.Delay(3000);
                retryCount--;
            }

            //transcribe file
            Console.WriteLine("Starting transcription.");
            await beey.TranscribeProjectAsync(project.Id, "cs-CZ", true, true, true, default);

            //wait for transcription
            Console.WriteLine("Waiting for transcription to finish, this may take a while.");
            retryCount = 20;
            //periodically checks if the server has finished a given process until true
            while ((result = await beey.GetProjectProgressStateAsync(project.Id, default).TryAsync())
                && !ProcessState.Finished.HasFlag(result.Value.PPCState)
                && retryCount > 0)
            {
                await Task.Delay(5000);
                retryCount--;
            }

            //download trsx file
            Console.WriteLine("Downloading trsx file.");
            //original is the trsx which is created from the track
            //current is the trsx which contains all the editing additions
            var stream = await beey.DownloadOriginalTrsxAsync(project.Id, default);

            byte[] trsx;
            using (var ms = new MemoryStream())
            {
                stream!.CopyTo(ms);
                trsx = ms.ToArray();
            }

            File.WriteAllBytes("transcribed.trsx", trsx);
        }
    }
}