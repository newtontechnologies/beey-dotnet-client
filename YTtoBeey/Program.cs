using System;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Diagnostics;
using Beey.Client;
using Beey.DataExchangeModel.Messaging;

namespace YTtoBeey
{
    class Program
    {
        static async Task Main(string[] args)
        {

            // Commandline args handling
            string configpath = "Settings.xml";
            bool attemptYT = false;
            string trsxPath = "transcript.trsx";

            if (args.Length < 1)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Usage: YTtoBeey.exe <path to mp3>/<video url> (<transcript.trsx>) (<Settings.xml>)");
                Console.ResetColor();
                return;
            }
            else if (args.Length == 1)
            {
                if (!File.Exists(args[0]))
                    attemptYT = true; //file not found, perhaps a url?
            }
            else if (args.Length == 2)
            {
                trsxPath = args[1];
            }
            else
            {
                trsxPath = args[1];
                configpath = args[2];
            }
            string video = args[0];

            // Connect & login to beey
            BeeyClient beey;
            try
            {
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

            // Create a project
            var project = await beey.CreateProjectAsync("Atest_" + DateTime.Now.ToFileTime().ToString(), "A/test");

            // Get stream to upload
            Stream upstream;

            string tmpfile = "";
            if (attemptYT)
            {
                // Attempt to start youtube-dl and download the video (supports more than only youtube)
                var proc = new Process();
                tmpfile = "temp-" + DateTime.Now.ToFileTime().ToString();
                //This would be much faster beey has issues with m4a: proc.StartInfo = new ProcessStartInfo("youtube-dl.exe", "--no-cache-dir -f bestaudio \"" + video + "\" --output " + tmpfile);
                proc.StartInfo = new ProcessStartInfo("youtube-dl.exe", "--no-cache-dir \"" + video + "\" --output " + tmpfile);
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.Start();
                Console.WriteLine("[INFO] Downloading video...");
                proc.WaitForExit();

                if (proc.StandardError.ReadToEnd().Contains("ERROR"))
                { //if youtube-dl returns error
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[FATAL] Could not download video. Check the url? Maybe video is age restricted?");
                    Console.ResetColor();
                    return;
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
                    return;
                }
            }
            else
            {
                upstream = new FileStream(video, FileMode.Open);
            }

            // Upload the stream
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
            await beey.TranscribeProjectAsync(project.Id);

            Console.Write("\n[INFO] Waiting for transcribing");
            while (
                (result = await beey.GetProjectProgressStateAsync(project.Id).TryAsync()) &&
                !ProcessState.Finished.HasFlag(result.Value.PPCState)
                )
            {
                Console.Write(".");
                await Task.Delay(1000);
            }

            // Download trsx
            Console.WriteLine("\n[INFO] Downloading trsx now");

            Stream downstream = await beey.DownloadOriginalTrsxAsync(project.Id);
            using (FileStream fs = new FileStream(trsxPath, FileMode.Create))
            {
                downstream!.CopyTo(fs);
            }
            Console.WriteLine("[INFO] Done!");
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
