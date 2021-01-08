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
    class Program
    {
        private static IConfiguration config;

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
                Log.Logger.Error("Please specify the file to be transcribed in the following format:\nthis.exe audio.mp4.");
                return;
            }
            else if (!File.Exists(args[0]))
            {
                Log.Logger.Error("Audio file not found. Please make sure that the file exists.");
                return;
            }
            else if (!File.Exists("Settings.xml"))
            {
                Log.Logger.Error("Settings.xml file not found. Please make sure that this file exists.");
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
            Log.Logger.Information("Logging in.");
            await beey.LoginAsync(email, password);

            //create project
            Log.Logger.Information("Creating project.");
            var project = await beey.CreateProjectAsync("projectname", "projectpath", default);

            //upload file
            Log.Logger.Information("Uploading file.");
            var fileStream = File.Open(audioPath, FileMode.Open);

            using var cts = new CancellationTokenSource();
            var uploading = beey.UploadStreamAsync(project.Id, "audioname", fileStream, fileStream.Length, false, cts.Token);
            var transcribing = BeeyHelper.TranscribeAsync(beey, project.Id,
                    language: "cs-CZ",
                    withPpc: true,
                    withVad: true,
                    withPunctuation: true,
                    saveTrsx: true,
                    onMediaIdentified: d => Log.Logger.Information("Duration is {d}", d),
                    onTranscriptionStarted: () => Log.Logger.Information("Transcription started"),
                    onUploadProgress: (b, p) => Log.Logger.Information("Upload: {p}% ({b}B)", p, b),
                    onTranscriptionProgress: p => Log.Logger.Information("Transcription: {p}%", p),
                    onUploadCompleted: () => Log.Logger.Information("Upload completed"),
                    onConversionCompleted: () => Log.Logger.Information("Conversion completed"),
                    onTranscriptionCompleted: () => Log.Logger.Information("Transcription completed"),
                    timeout: TimeSpan.FromSeconds(60),
                    cancellationToken: cts.Token);
            try
            {
                var allTasks = new List<Task>() { uploading, transcribing };
                while (allTasks.Any())
                {
                    var finished = await Task.WhenAny(allTasks);
                    if (!finished.IsCompletedSuccessfully)
                        await finished;
                    else
                        allTasks.Remove(finished);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Failed with error '{ex.Message}'.");
                cts.Cancel();
                _ = await Task.WhenAll(uploading, transcribing).TryAsync();
            }

            //download trsx file
            Log.Logger.Information("Downloading trsx file.");
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

            Log.Logger.Information("Finished");
        }
    }
}