using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public class ProcessIdEnricher : ILogEventEnricher
    {
        static ProcessIdEnricher()
        {
            _pid = Process.GetCurrentProcess().Id;
        }

        static int _pid { get; }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "ProcessId", _pid));
        }
    }
}
