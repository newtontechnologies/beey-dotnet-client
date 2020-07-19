using Beey.Client;
using Beey.DataExchangeModel.Messaging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace YTtoBeey
{
    class Program
    {
        private static BeeyClient beey;
        private static Beey.DataExchangeModel.Projects.Project project;
        
        private static bool transcribed = false;

        private const string verinfo = "YTtoBeey v1.2r0"; 

        static async Task Main(string[] args)
        {
            CleanTemp();
            Console.CancelKeyPress += Console_CancelKeyPress;

            // Commandline args handling
            string configpath = "Settings.xml";
            bool attemptYT = false;
            string trsxPath = "transcript.trsx";
            string language = "cs-CZ";
            string videouri = "";

            if (args.Length < 1)
            {
                Console.WriteLine("[INFO] " + verinfo);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Usage: YTtoBeey.exe <path to mp3>/<video url> (<transcript.trsx>) (<cs-CZ>) (<Settings.xml>)");
                Console.ResetColor();
                return;
            }
            else
            {
                if (!File.Exists(args[0]))
                    attemptYT = true; //file not found, perhaps a url?
                videouri = args[0];

                if (args.Length >= 2)
                    trsxPath = args[1];
                if (args.Length >= 3)
                    language = args[2];
                if (args.Length >= 4)
                    configpath = args[3];
            }

            // Connect & login to beey
            try
            {
                Console.WriteLine("[INFO] Login to beey..");
                beey = await LoadConfigAndConnect(configpath);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[FATAL] Configuration file not found!");
                Console.ResetColor();
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[FATAL] Login to beey failed.");
                Console.ResetColor();
                return;
            }

            // Get stream to upload
            Stream upstream;

            string tmpfile = "";
            if (attemptYT)
            {
                // Attempt to start youtube-dl and download the video (supports more than only youtube)
                var proc = new Process();
                tmpfile = "temp-" + DateTime.Now.ToFileTime().ToString();
                //This could be much faster; but beey has issues with m4a: proc.StartInfo = new ProcessStartInfo("youtube-dl.exe", "--no-cache-dir -f bestaudio \"" + videouri + "\" --output " + tmpfile);
                proc.StartInfo = new ProcessStartInfo("youtube-dl.exe", "--no-cache-dir \"" + videouri + "\" --output " + tmpfile);
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.Start();
                Console.WriteLine("[INFO] Downloading video...");
                proc.WaitForExit();

                { 
                    string stderr = proc.StandardError.ReadToEnd();

                    if (stderr.Contains("ERROR"))
                    { //if youtube-dl returns error
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[FATAL] Could not download video. Check the url? Maybe video is age restricted?");
                        Console.ResetColor();
                        Console.WriteLine("Full exception below:\n--\n"+stderr);
                        return;
                    }
                }
                
                try
                {
                    upstream = new FileStream(tmpfile, FileMode.Open);
                }
                catch (Exception ex)
                { // Just in case youtube-dl fails and doesn't output error.
                    Console.WriteLine(ex.ToString());
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[FATAL] Could not download video.");
                    Console.ResetColor();
                    Console.WriteLine("Full exception below:\n--\n" + ex.ToString());
                    return;
                }
            }
            else
            {
                upstream = new FileStream(videouri, FileMode.Open);
            }

            // Create a project
            Console.WriteLine("[INFO] Creating a project..");
            project = await beey.CreateProjectAsync("YTtoBeey_" + DateTime.Now.ToFileTime().ToString(), "YTtoBeey");

            // Upload the stream
            Console.WriteLine("[INFO] Uploading..");
            await beey.UploadStreamAsync(project.Id, "test01.mp3", upstream, null, false);

            // Close stream and delete temporary files
            upstream.Close();
            if (attemptYT)
                File.Delete(tmpfile);


            // Wait for transcoding
            TryValueResult<ProjectProgress> result;
            Console.Write("[INFO] Waiting for transcoding");
            while (
                (result = await beey.GetProjectProgressStateAsync(project.Id).TryAsync()) &&
                !ProcessState.Finished.HasFlag(result.Value.TranscodingState)
                )
            {
                Console.Write(".");
                await Task.Delay(1000);
            }

            // Wait for transcribing
            await beey.TranscribeProjectAsync(project.Id, language);

            Console.Write("\n[INFO] Waiting for transcribing");
            while (
                (result = await beey.GetProjectProgressStateAsync(project.Id).TryAsync()) &&
                !ProcessState.Finished.HasFlag(result.Value.PPCState)
                )
            {
                Console.Write(".");
                await Task.Delay(1000);
            }

            await Task.Delay(3000); //workaround, because beey sometimes say it's got trsx ready but it actually does not have it yet

            transcribed = true;

            // Download trsx
            Console.WriteLine("\n[INFO] Downloading trsx..");

            Stream downstream = null;

            short maxattemps = 5;
            for (int i = 1; i < maxattemps + 1; i++) //if download trsx fails, retry 5 times; workaround because beey sometimes has wierd issues
            {
                try
                {
                    downstream = await beey.DownloadOriginalTrsxAsync(project.Id);
                    break;
                }
                catch (Beey.Api.Rest.HttpException ex)
                {
                    if (i < maxattemps)
                        Console.WriteLine("[ERROR] Could not get TRSX file from Beey! ("+ex.HttpStatusCode+") Attempt " + i + "/" + maxattemps);
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[FATAL] Could not get TRSX file from Beey, check connection/try again.");
                        Console.ResetColor();
                        Console.WriteLine("Whole exception below:\n--\n");
                        Console.WriteLine(ex.ToString());
                        return;
                    }   
                }
                await Task.Delay(3000);
            }


            using (FileStream fs = new FileStream(trsxPath, FileMode.Create))
            {
                downstream!.CopyTo(fs);
            }
            Console.WriteLine("[INFO] Done!");
            CleanTemp();
        }

        /// <summary>
        /// Ctrl+C pressed, perform a cleanup and exit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static async void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n[FATAL] Ctrl+C was pressed, exiting...");
            Console.ResetColor();
            if(project != null && !transcribed)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[WARN] Project does not seem to be transcribed yet; deleting it..");
                Console.ResetColor();
                await beey.DeleteProjectAsync(project.Id);
            }
            CleanTemp();
            Environment.Exit(0);
        }

        /// <summary>
        /// Cleans up redundant temporary files
        /// </summary>
        static void CleanTemp()
        {
            string[] tmpfiles = Directory.GetFiles(".", "temp*", SearchOption.TopDirectoryOnly);
            if (tmpfiles.Length > 0)
            {
                Console.WriteLine("[INFO] Found redundant temporary files, cleaning up ...");
                foreach (string file in tmpfiles)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[ERROR] Failed to delete temporary file: " + file);
                        Console.ResetColor();
                    }
                }
            }
        }

        /// <summary>
        /// Loads up XML config and connects to beey server
        /// </summary>
        /// <param name="configpath">(optional) Path to Settings.xml</param>
        /// <returns>BeeyClient instance</returns>
        static async Task<BeeyClient> LoadConfigAndConnect(string configpath = "Settings.xml")
        {
            if (!File.Exists(configpath))
                throw new ArgumentException("File " + configpath + " doesn't exist!");

            var doc = new XmlDocument();
            doc.Load(configpath);
            var beey = new BeeyClient(doc.SelectSingleNode("/Settings/Beey-Server/Url").InnerText);
            await beey.LoginAsync(
                doc.SelectSingleNode("/Settings/Credentials/Email").InnerText,
                doc.SelectSingleNode("/Settings/Credentials/Password").InnerText
                );

            return beey;
        }
    }
}
