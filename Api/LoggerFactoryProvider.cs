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
    private static ILoggerFactory? _loggerFactory;
    public static ILoggerFactory LoggerFactory
    {
        get
        {
            if (_loggerFactory is null)
            {
                // Lots of Beey Apps use Serilog as log provider, 
                // so if Serilog is configured, use it.
                if (Log.Logger != Serilog.Core.Logger.None)
                {
                    _loggerFactory = new LoggerFactory().AddSerilog(Log.Logger, true);
                }
                else
                {
                    throw new InvalidOperationException($@"{nameof(LoggerFactory)} is not set and Serilog is not configured.
Because of problems with LibLog, we moved to Microsoft.Extensions.Logging, but we still need a static logger,
so please set the {nameof(LoggerFactoryProvider)}.{nameof(LoggerFactory)} property before using the library.");
                }
            }
            return _loggerFactory;
        }
        set
        {
            _loggerFactory = value;
        }
    }
}
