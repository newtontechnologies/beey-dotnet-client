using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;

namespace BeeyApi.WebSockets
{
    public class WebSocketClosedException : Exception
    {
        public WebSocketCloseStatus? CloseStatus { get; private set; }
        public string? CloseMessage { get; private set; }       

        public WebSocketClosedException(WebSocketException ex, WebSocketCloseStatus? closeStatus, string? closeMessage)
            : base(ex.Message, ex)
        {
            CloseStatus = closeStatus;
            CloseMessage = closeMessage;
        }

    }
}
