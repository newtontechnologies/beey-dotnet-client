using BeeyApi.WebSockets;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Text;


namespace BeeyApi
{
    internal class Utility
    {
        internal static void LogApiException(Exception ex, Logging.ILog logger)
        {
            if (ex is WebSocketClosedException wsEx && wsEx.CloseStatus.HasValue)
            {
                logger.Log(Logging.LogLevel.Error, () => $"Error in Beey API, WebSocket closed ({wsEx.CloseStatus?.ToString()}) with message '{wsEx.Message}'.", wsEx);
            }
            else
            {
                logger.Log(Logging.LogLevel.Error, () => $"Error in Beey API: '{ex.Message}'", ex);
            }           
        }
    }
}
