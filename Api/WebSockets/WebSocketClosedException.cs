﻿using System;
using System.Net.WebSockets;

namespace Beey.Api.WebSockets;

public class WebSocketClosedException : Exception
{
    public WebSocketCloseStatus? CloseStatus { get; private set; }     

    public WebSocketClosedException(string? closeMessage, WebSocketCloseStatus? closeStatus, WebSocketException? ex)
        : base(closeMessage ?? ex.Message, ex)
    {
        CloseStatus = closeStatus;
    }

}
