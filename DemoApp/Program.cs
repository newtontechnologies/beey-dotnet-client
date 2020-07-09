using Beey.Api.Rest;
using Beey.Client;
using Beey.DataExchangeModel.Messaging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using TranscriptionCore;

namespace DemoApp
{
    class Program
    {
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

            //command line arguments: DemoApp.exe settings.xml audio.mp4
            var cmdArgs = Environment.GetCommandLineArgs();
            if (cmdArgs.Length < 3) {
                Console.WriteLine("Please specify the settings file and the file to be transcribed in the following format:\nthis.exe settings.xml audio.mp4.");
                Environment.Exit(1);
            } else if (!File.Exists(cmdArgs[1])) {
                Console.WriteLine("Settings file not found. Please make sure that the file exists.");
                Environment.Exit(1);
            } else if (!File.Exists(cmdArgs[2])) {
                Console.WriteLine("Audio file not found. Please make sure that the file exists.");
                Environment.Exit(1);
            }

            //load settings
            string audioPath = cmdArgs[2];
            string url = "";
            string email = "";
            string password = "";

            //use xml for scalability
            XmlReader xmlReader = XmlReader.Create(cmdArgs[1]);
            while (xmlReader.Read()) {
                if ((xmlReader.NodeType == XmlNodeType.Element)) {
                    string caseSwitch = xmlReader.Name;
                    switch (caseSwitch) {
                        case "Url":
                            url = xmlReader.ReadElementContentAsString();
                            break;
                        case "Email":
                            email = xmlReader.ReadElementContentAsString();
                            break;
                        case "Password":
                            password = xmlReader.ReadElementContentAsString();
                            break;
                        default:
                            break;
                    }
                }
            }

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
                && retryCount > 0) {
                await Task.Delay(5000);
                retryCount--;
            }

            //download trsx file
            Console.WriteLine("Downloading trsx file.");
            //original is the trsx which is created from the track
            //current is the trsx which contains all the editing additions
            var stream = await beey.DownloadOriginalTrsxAsync(project.Id, default);

            byte[] trsx;
            using (var ms = new MemoryStream()) {
                stream!.CopyTo(ms);
                trsx = ms.ToArray();
            }

            File.WriteAllBytes("transcribed.trsx", trsx);
        }
    }
}