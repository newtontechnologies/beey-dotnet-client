using Beey.Client;
using Beey.DataExchangeModel.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace BatchTranscriber
{
    enum WorkerStatus { READY = 0, WORKING = 2, FINISHED = 3, FAILED = -1 };

    class Program
    {
        const string verinfo = "BatchTranscriber v1.0r0";

        static int threadlimit = 8; //Number of threads to run at the same time.
        static string outputdir = "out"; //Output directory
        static string settingsfile = "Settings.xml";
        static string language = "cs-CZ";

        static bool usingInputDir = true; //using directory as input or a file list?
        static string inputdir = ".";
        static string inputfile = "list.txt";
        static bool singlemode = false; //batch or a single file? 
        static bool debug = false; //true if you want to spam the console with debug info.

        static List<string> files = new List<string>(); //list of files to transcribe

        static Thread[] threads; // thread pool
        static WorkerStatus[] threadStatuses; // used for managing threads

        static BeeyClient beey;

        static async Task Main(string[] args)
        {
            HandleArgs(args);

            PrepareFileList();

            try
            {
                beey = await LoadConfigAndConnect(settingsfile);
            }
            catch (ArgumentException ex)
            {
                if (debug)
                    Console.WriteLine(ex.ToString());
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[FATAL] Configuration file not found!");
                Console.ResetColor();
                return;
            }
            catch (Exception ex)
            {
                if (debug)
                    Console.WriteLine(ex.ToString());
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[FATAL] Login to beey failed.");
                Console.ResetColor();
                return;
            }

            //Init threadpool
            threads = new Thread[threadlimit];
            threadStatuses = new WorkerStatus[threadlimit];

            //Start the work
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            #region Work

            int currfile = 0; //Pointer in file list
            bool startnew = true; //start new threads?
            bool allfinished = true; //are all threads finished?

            while (!(!startnew && allfinished)) //repeat until all files are processed and all threads are finished
            {
                allfinished = true;

                for (int i = 0; i < threadlimit; i++) //cycle through threads
                {
                    if (currfile >= files.Count)
                        startnew = false; //all files processed, dont start new threads

                    if (threadStatuses[i] == WorkerStatus.READY && startnew)
                    {
                        //the thread is ready for a new job
                        threads[i] = new Thread(() => Work(files[currfile], i));
                        threadStatuses[i] = WorkerStatus.WORKING;
                        threads[i].Start();
                        Console.WriteLine("Thread #" + i + ": Started. (" + files[currfile] + ")");
                        Thread.Sleep(400); //give it some time (otherwise it would do wierd magical things)
                        currfile++;
                        allfinished = false;
                    }
                    else if (threadStatuses[i] == WorkerStatus.WORKING)
                    {
                        //thread is working
                        allfinished = false;
                    }
                    else if (threadStatuses[i] == WorkerStatus.FINISHED)
                    { //thread has finished
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Thread #" + i + ": Finished.");
                        Console.ResetColor();
                        threadStatuses[i] = WorkerStatus.READY; //set status ready
                    }
                    else if (threadStatuses[i] == WorkerStatus.FAILED)
                    { //thread failed
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Thread #" + i + ": Failed.");
                        Console.ResetColor();
                        threadStatuses[i] = WorkerStatus.READY; //set status ready
                    }
                }

                Thread.Sleep(1000); //wait, then cycle through threads again
            }
            #endregion

            stopwatch.Stop();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[INFO] Finished in " + stopwatch.Elapsed);
            Console.ResetColor();
        }

        /// <summary>
        /// Prints out "[FATAL] ..." in red color and exits the program.
        /// </summary>
        /// <param name="msg">Message</param>
        static void Fatal(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[FATAL] " + msg);
            Console.ResetColor();
            Environment.Exit(-1);
        }

        /// <summary>
        /// Handles all the commandline arguments and assigns vars.
        /// </summary>
        /// <param name="args">cmdline args</param>
        static void HandleArgs(string[] args)
        {
            if (args.Length < 2)
            {
                //print out help
                Console.WriteLine("[INFO] " + verinfo);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Usage: BatchTranscriber.exe {<directory with audio files>|<list of files>} <output directory> (threads=8) (settings=Settings.xml) (mode={batch|single}) (debug={no|yes})");
                Console.WriteLine("\n<list of files> == Filename of a file which contains paths to files to transcribe (one on each line)");
                Console.WriteLine("If mode=single is set, then the first argument will be treated like a single file.");
                Console.ResetColor();
                Environment.Exit(0);
            }

            if (File.Exists(args[0]))
            { // arg0 is a file list
                inputfile = args[0];
                usingInputDir = false;
            }
            else if (Directory.Exists(args[0])) //arg0 is a directory
                inputdir = args[0];
            else
                Fatal("Could not find '" + args[0] + "'."); //arg0 is nonsense

            outputdir = args[1]; //arg1 will be the output directory
            if (!Directory.Exists(outputdir))
                Directory.CreateDirectory(outputdir);


            for (int i = 2; i < args.Length; i++) //cycle through remaining args, looking for optional settings
            {
                string[] argsplit = args[i].Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (argsplit.Length < 2)
                {
                    Console.WriteLine("[WARN] '" + args[i] + "' is an invalid argument.");
                    continue;
                }
                switch (argsplit[0])
                {
                    case "threads":
                        if (!int.TryParse(argsplit[1], out threadlimit)) //threadlimit
                            Console.WriteLine("[WARN] '" + args[i] + "' is an invalid argument.");
                        break;
                    case "mode":
                        if (argsplit[1] == "single") //single/batch mode
                            singlemode = true;
                        break;
                    case "settings":
                        settingsfile = args[1]; //custom Settings.xml
                        break;
                    case "debug":
                        if (argsplit[1] == "yes") //spam the console with debug info?
                            debug = true;
                        break;
                }
            }
            if (threadlimit < 1)
                Fatal("Thread limit must be 1 or higher.");
        }

        /// <summary>
        /// Prepares file list with path to files to transcribe
        /// </summary>
        static void PrepareFileList()
        {
            if (usingInputDir)
            {
                foreach (string file in Directory.GetFiles(inputdir))
                    files.Add(file);
            }
            else if (singlemode)
            {
                files.Add(inputfile);
            }
            else
            {
                using (StreamReader r = new StreamReader(inputfile))
                {
                    string line;
                    while ((line = r.ReadLine()) != null)
                        files.Add(line);
                }
            }
        }

        /// <summary>
        /// Loads up Settings.xml and logins to beey
        /// </summary>
        /// <param name="configpath">Path to Settings.xml</param>
        /// <returns>BeeyClient instance with login done</returns>
        static async Task<BeeyClient> LoadConfigAndConnect(string configpath = "Settings.xml")
        {
            if (!File.Exists(configpath))
                throw new ArgumentException("File " + configpath + " doesn't exist!");

            var doc = new XmlDocument();
            doc.Load(configpath);
            language = doc.SelectSingleNode("/Settings/Language").InnerText;
            var beey = new BeeyClient(doc.SelectSingleNode("/Settings/Beey-Server/Url").InnerText);
            await beey.LoginAsync(
                doc.SelectSingleNode("/Settings/Credentials/Email").InnerText,
                doc.SelectSingleNode("/Settings/Credentials/Password").InnerText
                );

            return beey;
        }

        /// <summary>
        /// Uploads a file and downloads its trsx
        /// </summary>
        /// <param name="file">path to the file</param>
        /// <param name="threadId">id of the thread in pool</param>
        /// <returns></returns>
        static async Task Work(string file, int threadId)
        {
            threadStatuses[threadId] = WorkerStatus.WORKING;

            if (debug)
                Console.WriteLine("[DEBUG] Thread " + threadId + ": Begin creating project.");

            // Create a project
            var project = await beey.CreateProjectAsync("BatchTranscriber_" + DateTime.Now.ToFileTime().ToString(), "A/test");

            // Open a filestream
            Stream upstream;

            try
            {
                upstream = new FileStream(file, FileMode.Open);
            }
            catch (Exception ex)
            {
                threadStatuses[threadId] = WorkerStatus.FAILED; //fail if file not found
                if (debug)
                    Console.WriteLine("[DEBUG] Thread " + threadId + ": Exception thrown while opening file:\n" + ex.ToString());
                return;
            }

            // Upload the stream
            if (debug)
                Console.WriteLine("[DEBUG] Thread " + threadId + ": Begin upload stream.");

            await beey.UploadStreamAsync(project.Id, "test01.mp3", upstream, null, false);

            // Close stream
            upstream.Close();

            if (debug)
                Console.WriteLine("[DEBUG] Thread " + threadId + ": Stream uploaded.");

            // Wait for transcoding
            TryValueResult<ProjectProgress> result;
            while (
                (result = await beey.GetProjectProgressStateAsync(project.Id).TryAsync()) &&
                !ProcessState.Finished.HasFlag(result.Value.TranscodingState)
                )
            {
                /*if (debug)
                    Console.WriteLine("[DEBUG] Thread " + threadId + ": Wait for transcode.");*/
                await Task.Delay(1000);
            }

            if (debug)
                Console.WriteLine("[DEBUG] Thread " + threadId + ": Transcode finished.");

            // Wait for transcribing
            await beey.TranscribeProjectAsync(project.Id, language);

            if (debug)
                Console.WriteLine("[DEBUG] Thread " + threadId + ": Transcribing.");

            while (
                (result = await beey.GetProjectProgressStateAsync(project.Id).TryAsync()) &&
                !ProcessState.Finished.HasFlag(result.Value.PPCState)
                )
            {
                /*if (debug)
                    Console.WriteLine("[DEBUG] Thread " + threadId + ": Wait for transcribe.");*/
                await Task.Delay(1000);
            }

            // Download trsx

            await Task.Delay(3000); //workaround so hopefully the mysterious bug wont happen as it happened in YTtoBeey

            if (debug)
                Console.WriteLine("[DEBUG] Thread " + threadId + ": Begin download");

            Stream downstream = null;
            try { 
                downstream = await beey.DownloadOriginalTrsxAsync(project.Id);
            } catch(Exception ex) {
                threadStatuses[threadId] = WorkerStatus.FAILED;
                if (debug)
                    Console.WriteLine("[DEBUG] Thread " + threadId + ": Exception while requesting trsx, full below:\n--\n" + ex.ToString() + "\n--\n");
            }

            if (debug)
                Console.WriteLine("[DEBUG] Thread " + threadId + ": Downloading");

            try
            {

                using (FileStream fs = new FileStream(outputdir + "\\" + Path.GetFileName(file) + ".trsx", FileMode.Create))
                    downstream!.CopyTo(fs);

            }
            catch (Exception ex)
            {
                threadStatuses[threadId] = WorkerStatus.FAILED; //fail while opening a file to download trsx into
                if (debug)
                    Console.WriteLine("[DEBUG] Thread " + threadId + ": Exception thrown while downloading trsx:\n--\n" + ex.ToString() + "\n--\n");
                return;
            }

            if (debug)
                Console.WriteLine("[DEBUG] Thread " + threadId + ": Finished.");

            threadStatuses[threadId] = WorkerStatus.FINISHED; //set status as finished
        }
    }
}
