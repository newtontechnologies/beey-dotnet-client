using Beey.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranscriptionCore;

namespace SpeakerDbUpdater
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
            string login = "milos.kudelka@newtontech.cz";
            string password = "OVPgod";
            string speakerFile = @"c:\Users\milos.kudelka\Downloads\Production\tvr_add.ini";
            bool checkAndRemoveDuplicitiesInDb = true;

            var beey = new BeeyClient(url);
            await beey.LoginAsync(login, password);

            var fileSpeakers = GetSpeakers(speakerFile);
            Log.Information($"Found {fileSpeakers.Count} Speakers in file.");

            if (fileSpeakers.Any())
            {
                Log.Information("Loading current Speakers from DB.");
                var dbSpeakers = GetSpeakers(beey);

                Log.Information($"Inserting new Speakers.");
                var (newSpeakers, notNewSpeakers) = SelectNewSpeakers(fileSpeakers, dbSpeakers);
                var notInserted = InsertSpeakersToDb(newSpeakers, beey);
                Log.Information($"Successfuly inserted {newSpeakers.Count - notInserted.Count} new Speakers.");

                var builder = new StringBuilder();
                builder.AppendLine("Speakers already in DB:");
                foreach (var notNewSpeaker in notNewSpeakers)
                {
                    builder.AppendLine("\t" + notNewSpeaker.Serialize().ToString());
                }
                Log.Verbose(builder.ToString());

                if (notInserted.Any())
                {
                    Log.Warning($"{notInserted.Count} Speakers failed to be inserted.");

                    builder.Clear().AppendLine();
                    foreach (var speaker in notInserted)
                    {
                        builder.AppendLine("\t" + speaker.Serialize().ToString());
                    }
                    Log.Verbose(builder.ToString());
                }

                if (checkAndRemoveDuplicitiesInDb)
                {
                    dbSpeakers = GetSpeakers(beey);
                    Log.Information("Searching for duplicit Speakers in DB.");
                    var dbDuplicities = FindDuplicities(dbSpeakers);
                    Log.Information($"Found {dbDuplicities.Count} Speakers with duplicities.");

                    if (dbDuplicities.Any())
                    {
                        Log.Information("Removing duplicit Speakers.");
                        var removed = RemoveDuplicitiesFromDb(dbDuplicities, beey);

                        if (removed.Any())
                        {
                            var msg = new StringBuilder().AppendLine();

                            int allDuplicities = 0;
                            int removedDuplicities = 0;
                            foreach (var speaker in removed)
                            {
                                if (speaker.Value.Success.Any() || speaker.Value.Fail.Any())
                                {
                                    allDuplicities += speaker.Value.Success.Count + speaker.Value.Fail.Count;
                                    removedDuplicities += speaker.Value.Success.Count;
                                    msg.AppendLine($"{speaker.Key}: {speaker.Value.Success.Count} duplicit Speakers, removed {speaker.Value.Success.Count} out of {speaker.Value.Fail.Count + speaker.Value.Success.Count}.");
                                }
                            }
                            Log.Information($"Successfuly removed {removedDuplicities} duplicitites.");
                            Log.Error($"Failed to remove {allDuplicities - removedDuplicities} duplicities.");

                            Log.Verbose(msg.ToString());
                        }
                    }
                }
            }
            Log.Information("Update finished.");
        }

        static (int totalCount, List<Speaker> failed) UpdateDb(List<Speaker> newSpeakers, BeeyClient beey)
        {
            var notInserted = InsertSpeakersToDb(newSpeakers, beey);

            return (newSpeakers.Count(), notInserted);
        }

        static Dictionary<string, (List<string> Success, List<string> Fail)> RemoveDuplicitiesFromDb(List<IEnumerable<Speaker>> dbDuplicitites, BeeyClient beey)
        {
            var removedSpeakersIds = new Dictionary<string, (List<string> Success, List<string> Fail)>();
            foreach (var duplicity in dbDuplicitites)
            {
                var firstSpeaker = duplicity.First();
                var duplicitSpeakersToDelete = duplicity.Skip(1);
                foreach (var speaker in duplicitSpeakersToDelete)
                {
                    removedSpeakersIds.Add(firstSpeaker.DBID, (new List<string>(), new List<string>()));

                    if (beey.DeleteSpeakerAsync(firstSpeaker.DBID).TryAsync().Result)
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

        /// <summary>
        /// Returns speakers, that were not inserted, or empty list.
        /// </summary>
        /// <param name="speakers"></param>
        /// <param name="speakerApi"></param>
        /// <returns></returns>
        static List<Speaker> InsertSpeakersToDb(IEnumerable<Speaker> speakers, BeeyClient beey)
        {
            if (beey == null || speakers == null)
            {
                throw new ArgumentException();
            }

            var unsuccessfullyInserted = new List<Speaker>();
            foreach (var speaker in speakers)
            {
                Speaker? newSpeaker = null;
                try
                {
                    newSpeaker = beey.CreateSpeakerAsync(speaker).Result;
                }
                catch (Exception)
                {
                    // do nothing
                }

                if (newSpeaker == null)
                {
                    unsuccessfullyInserted.Add(speaker);
                }
            }

            return unsuccessfullyInserted;
        }

        static (List<Speaker> newSpeakers, List<Speaker> notNewSpeakers) SelectNewSpeakers(List<Speaker> speakersToAdd, List<Speaker> currentSpeakers)
        {
            var speakersToAddDict = speakersToAdd.ToDictionary(s => s.FullName);

            var newSpeakers = speakersToAddDict.Keys
                .Except(currentSpeakers.Select(s => s.FullName));
            var notNewSpeakers = speakersToAddDict.Keys.Except(newSpeakers);

            return (newSpeakers.Select(s => speakersToAddDict[s]).ToList(),
                notNewSpeakers.Select(s => speakersToAddDict[s]).ToList());
        }

        static List<Speaker> GetSpeakers(BeeyClient beey)
        {
            var result = new List<Speaker>();
            int count = 1000;
            int skip = 0;

            var speakers = beey.ListSpeakersAsync(count, skip).Result;
            while (speakers != null && speakers.ListedCount > 0)
            {
                result.AddRange(speakers.List);
                skip += count;
                speakers = beey.ListSpeakersAsync(count, skip).Result;
            }

            return result;
        }

        static List<Speaker> GetSpeakers(string file)
        {
            var ini = SpeakerIniParser.Parse(file);

            var speakers = ini.Select((s, si) =>
            {
                var name = s.SectionName.Split(';');

                return new Speaker()
                {
                    Surname = name[0],
                    FirstName = name.Length > 1 ? name[1] : null,
                    Attributes = s.Keys.Select((kd, i) => new SpeakerAttribute(i.ToString(), "role", kd)).ToList()
                };
            });

            return speakers.ToList();
        }

        /// <summary>
        /// Simple and dumb equality checking.
        /// </summary>
        /// <param name="speaker1"></param>
        /// <param name="speaker2"></param>
        /// <returns></returns>
        private static bool IsDuplicit(Speaker speaker1, Speaker speaker2)
        {
            bool result = speaker1.FullName == speaker2.FullName;
            result = result
                && speaker1.Attributes.Select(a => a.Value)
                    .Intersect(speaker2.Attributes.Select(a => a.Value))
                    .Any();

            return result;
        }

        /// <summary>
        /// Finds speakers with duplicitites only among suspected speakers.
        /// </summary>
        /// <param name="speakers"></param>
        /// <param name="suspectedWithDuplicities"></param>
        /// <returns></returns>
        static List<IEnumerable<Speaker>> FindDuplicities(List<Speaker> speakers)
        {
            var uniqueSpeakers = speakers.Distinct((CustomEqualityComparer<Speaker>)IsDuplicit);
            var speakersWithoutUnique = speakers.Except(uniqueSpeakers);
            var result = new List<IEnumerable<Speaker>>();
            foreach (var speaker in uniqueSpeakers)
            {
                var duplicits = speakersWithoutUnique.Where(s => IsDuplicit(speaker, s)).ToList();
                if (duplicits.Count > 1)
                {
                    result.Add(duplicits);
                }
            }

            return result;
        }
    }
}
