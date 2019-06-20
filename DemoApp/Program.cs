using Beey.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TranscriptionCore;

namespace DemoApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("beey.log")
                .CreateLogger();

            string url = "http://localhost:61497";

            var beey = new BeeyClient(url);
            await beey.LoginAsync("milos.kudelka@newtontech.cz", "OVPgod");

            if (false)
            {
                string speakerFile = @"..\..\..\tvrlidi.ini";
                var notInserted = UpdateDatabase(speakerFile, beey);
                var removed = RemoveDbDuplicitiesFromFile(speakerFile, beey);
            }
        }

        static List<Speaker> UpdateDatabase(string speakerFile, BeeyClient beey)
        {
            var newSpeakers = SpeakerUpdater.GetNewSpeakers(speakerFile, beey);
            var notInserted = SpeakerUpdater.InsertNewSpeakers(newSpeakers, beey);

            return notInserted;
        }

        static Dictionary<string, (List<string> Success, List<string> Fail)> RemoveDbDuplicitiesFromFile(string speakerFile, BeeyClient beey)
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
