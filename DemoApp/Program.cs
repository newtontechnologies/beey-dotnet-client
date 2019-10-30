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
            var a = AppDomain.CurrentDomain.BaseDirectory;
            a = Path.Combine(a, "..", "..", "..");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("beey.log")
                .CreateLogger();

            string url = "http://localhost:61497";

            var beey = new BeeyClient(url);
            await beey.LoginAsync("milos.kudelka@newtontech.cz", "OVPgod");
        }        
    }
}
