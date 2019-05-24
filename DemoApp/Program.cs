using BeeyApi;
using BeeyUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TranscriptionCore;

namespace DemoApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("beey.log")
                .CreateLogger();

            string url = "http://localhost:61497";
            string? strResult = "";
            bool bResult = true;

            var beey = new Beey(url);
            strResult = beey.LoginAsync("milos.kudelka@newtontech.cz", "OVPgod").Result;

            //string speakerFile = @"..\..\..\tvrlidi.ini";
            //SpeakerUpdater.LoadSpeakers(speakerFile);
            //UpdateDatabase(speakerFile, beey);
            //var removed = RemoveDbDuplicitiesFromFile(speakerFile, beey);

            var speakers = beey.ListSpeakersAsync(100).Result;
            var speaker = beey.GetSpeakerAsync(speakers?.List.FirstOrDefault()?.DBID ?? "").Result;

            var projects = beey.ListProjectsAsync(100).Result;
            var project = beey.GetProjectAsync(projects?.List.FirstOrDefault()?.Id ?? -1).Result;

            var mp3Path = @"c:\Users\milos.kudelka\Downloads\test01.mp3";
            bResult = beey.UploadFileAsync(project?.Id ?? -1, new FileInfo(mp3Path), "cz", false).Result;
        }

        static void UpdateDatabase(string speakerFile, Beey beey)
        {
            var newSpeakers = SpeakerUpdater.GetNewSpeakers(speakerFile, beey);
            var notInserted = SpeakerUpdater.InsertNewSpeakers(newSpeakers, beey);
        }

        static Dictionary<string, (List<string> Success, List<string> Fail)> RemoveDbDuplicitiesFromFile(string speakerFile, Beey beey)
        {
            var dbDuplicitiesFromFile = SpeakerUpdater.FindDbDuplicitiesFromFile(speakerFile, beey);

            var removedSpeakersIds = new Dictionary<string, (List<string> Success, List<string> Fail)>();
            foreach (var duplicity in dbDuplicitiesFromFile)
            {
                var firstSpeaker = duplicity.First();
                var duplicitSpeakersToDelete = duplicity.Skip(1);
                foreach (var speaker in duplicitSpeakersToDelete)
                {
                    removedSpeakersIds.Add(firstSpeaker.DBID, (new List<string>(), new List<string>()));

                    if (beey.DeleteSpeakerAsync(firstSpeaker.DBID).Result)
                    {
                        removedSpeakersIds[firstSpeaker.DBID].Success.Add(speaker.DBID);
                    }
                    else
                    {
                        removedSpeakersIds[speaker.DBID].Fail.Add(speaker.DBID);
                    }
                }
            }

            return removedSpeakersIds;
        }
    }
}
