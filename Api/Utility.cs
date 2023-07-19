using Beey.Api.WebSockets;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Text;


namespace Beey.Api;

internal class Utility
{
    internal static void LogApiException(Exception ex, ILogger logger)
    {
        if (ex is WebSocketClosedException wsEx && wsEx.CloseStatus.HasValue)
        {
            logger.LogError(
                wsEx,
                "Error in Beey API, WebSocket closed ({closedStatus}) with message '{message}'.",
                wsEx.CloseStatus?.ToString(),
                wsEx.Message);
        }
        else
        {
            logger.LogError(ex, "Error in Beey API: '{message}'", ex.Message);
        }
    }
}
