using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Beey.Api;

// TODO: Refactor library to inject ILogger and delete this class, if needed.
public static class LoggerFactoryProvider
{
    public static ILoggerFactory? LoggerFactory { get; set; }

    static LoggerFactoryProvider()
    {
        if (LoggerFactory is null)
        {
            // Lots of Beey Apps use Serilog as log provider, 
            // so if Serilog is configured, use it.
            if (Log.Logger != Serilog.Core.Logger.None)
            {
                LoggerFactory = new LoggerFactory().AddSerilog(Log.Logger, true);
            }
            else
            {
                throw new InvalidOperationException($@"{nameof(LoggerFactory)} is not set and Serilog is not configured.
Because of problems with LibLog, we moved to Microsoft.Extensions.Logging, but we still need a static logger,
so please set the {nameof(LoggerFactoryProvider)}.{nameof(LoggerFactory)} property before using the library.");
            }
        }
    }
}
