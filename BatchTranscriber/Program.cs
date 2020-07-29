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
        const string versionInfo = "BatchTranscriber v1.1r1";

        static int maxThreads = 8; //Number of threads to run at the same time.
        static string outputDirectory = "out"; //Output directory
        static string settingsFile = "Settings.xml";
        static string language = null;

        static bool usingInputDir = true; //using directory as input or a file list?
        static string inputDirectory = ".";
        static string inputFileList = "list.txt";
        static bool singlemode = false; //batch or a single file? 
        static bool debug = false; //true if you want to spam the console with debug info.

        static List<string> files = new List<string>(); //list of files to transcribe

        static Thread[] threads; // thread pool
        static WorkerStatus[] threadStatuses; // used for managing threads

        static BeeyClient beey;

        static string loginToken = null;

        static async Task Main(string[] args)
        {
            HandleArgs(args);

            PrepareFileList();

            try
            {
                beey = await LoadConfigAndConnect(settingsFile,loginToken);
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
            threads = new Thread[maxThreads];
            threadStatuses = new WorkerStatus[maxThreads];

            //Start the work
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            #region Work

            int currentFileIndex = 0;
            bool filesAreRemaining = true;
            bool allThreadsFinished = true;

            while (filesAreRemaining || !allThreadsFinished)
            {
                allThreadsFinished = true;

                for (int i = 0; i < maxThreads; i++) //cycle through threads
                {
                    if (currentFileIndex >= files.Count)
                        filesAreRemaining = false;

                    if (threadStatuses[i] == WorkerStatus.READY && filesAreRemaining)
                    {
                        threads[i] = new Thread(() => Work(files[currentFileIndex], i));
                        threadStatuses[i] = WorkerStatus.WORKING;
                        threads[i].Start();
                        Console.WriteLine("Thread #" + i + ": Started. (" + files[currentFileIndex] + ")");
                        Thread.Sleep(400); //give it some time (otherwise it would do weird magical things)
                        currentFileIndex++;
                        allThreadsFinished = false;
                    }
                    else if (threadStatuses[i] == WorkerStatus.WORKING)
                    {
                        allThreadsFinished = false;
                    }
                    else if (threadStatuses[i] == WorkerStatus.FINISHED)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Thread #" + i + ": Finished.");
                        Console.ResetColor();
                        threadStatuses[i] = WorkerStatus.READY;
                    }
                    else if (threadStatuses[i] == WorkerStatus.FAILED)
                    { //thread failed
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Thread #" + i + ": Failed.");
                        Console.ResetColor();
                        threadStatuses[i] = WorkerStatus.READY;
                    }
                }

                Thread.Sleep(1000);
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
                Console.WriteLine("[INFO] " + versionInfo);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Usage: BatchTranscriber.exe {<directory with audio files>|<list of files>} <output directory> (threads=8) (logintoken=TOKEN) (settings=Settings.xml) (mode={batch|single}) (debug={no|yes}) (language=cs-CZ)");
                Console.WriteLine("\n<list of files> == Filename of a file which contains paths to files to transcribe (one on each line)");
                Console.WriteLine("If mode=single is set, then the first argument will be treated like a single file.");
                Console.ResetColor();
                Environment.Exit(0);
            }

            if (File.Exists(args[0]))
            { // arg0 is a file list
                inputFileList = args[0];
                usingInputDir = false;
            }
            else if (Directory.Exists(args[0])) //arg0 is a directory
                inputDirectory = args[0];
            else
                Fatal("Could not find '" + args[0] + "'."); //arg0 is nonsense

            outputDirectory = args[1]; //arg1 will be the output directory
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);


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
                        if (!int.TryParse(argsplit[1], out maxThreads))
                            Console.WriteLine("[WARN] '" + args[i] + "' is an invalid argument.");
                        break;
                    case "mode":
                        if (argsplit[1] == "single")
                            singlemode = true;
                        break;
                    case "settings":
                        settingsFile = args[1];
                        break;
                    case "debug":
                        if (argsplit[1] == "yes")
                            debug = true;
                        break;
                    case "logintoken":
                        loginToken = argsplit[1];
                        break;
                    case "language":
                        language = argsplit[1];
                        break;
                }
            }
            if (maxThreads < 1)
                Fatal("Thread limit must be 1 or higher.");
        }

        /// <summary>
        /// Prepares file list with path to files to transcribe
        /// </summary>
        static void PrepareFileList()
        {
            if (usingInputDir)
            {
                foreach (string file in Directory.GetFiles(inputDirectory))
                    files.Add(file);
            }
            else if (singlemode)
            {
                files.Add(inputFileList);
            }
            else
            {
                using (StreamReader r = new StreamReader(inputFileList))
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
        /// <param name="token">Will be used as alternative to email/passwd if not null</param>
        /// <returns>BeeyClient instance with login done</returns>
        static async Task<BeeyClient> LoadConfigAndConnect(string configpath = "Settings.xml", string token = null)
        {
            if (!File.Exists(configpath))
                throw new ArgumentException("File " + configpath + " doesn't exist!");

            var doc = new XmlDocument();
            doc.Load(configpath);

            if(language == null)
                language = doc.SelectSingleNode("/Settings/Language").InnerText;

            var beey = new BeeyClient(doc.SelectSingleNode("/Settings/Beey-Server/Url").InnerText);
            if (token == null)
                await beey.LoginAsync(
                    doc.SelectSingleNode("/Settings/Credentials/Email").InnerText,
                    doc.SelectSingleNode("/Settings/Credentials/Password").InnerText
                    );
            else
                await beey.LoginAsync(token);

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

            Beey.DataExchangeModel.Projects.Project project;
            try { 
                project = await beey.CreateProjectAsync("BatchTranscriber_" + DateTime.Now.ToFileTime().ToString(), "A/test");
            } catch(Exception ex) {
                threadStatuses[threadId] = WorkerStatus.FAILED;
                if (debug)
                    Console.WriteLine("[DEBUG] Thread " + threadId + ": Exception below\n--\n"+ex.ToString()+"--\n\n");
                return;
            }
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

            
            if (debug)
                Console.WriteLine("[DEBUG] Thread " + threadId + ": Begin upload stream.");

            await beey.UploadStreamAsync(project.Id, "test01.mp3", upstream, null, false);

            
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

            try {
                using (FileStream fs = new FileStream(outputDirectory + "\\" + Path.GetFileName(file) + ".trsx", FileMode.Create))
                    downstream!.CopyTo(fs);
            }
            catch (Exception ex)
            {
                threadStatuses[threadId] = WorkerStatus.FAILED;
                if (debug)
                    Console.WriteLine("[DEBUG] Thread " + threadId + ": Exception thrown while downloading trsx:\n--\n" + ex.ToString() + "\n--\n");
                return;
            }

            if (debug)
                Console.WriteLine("[DEBUG] Thread " + threadId + ": Finished.");

            threadStatuses[threadId] = WorkerStatus.FINISHED;
        }
    }
}
