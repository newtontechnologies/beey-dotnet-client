using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Beey.Api.WebSockets;

namespace Beey.Api;

public static class Helpers
{
    /// <summary>
    /// fill buffer with entire message (from start)
    /// </summary>
    /// <returns>bytes written to buffer and last processed message result, when !EndOfMessage buffer is filled before end of message was received</returns>
    public static async Task<(int bytes, ValueWebSocketReceiveResult lastResult)> ReceiveMessageAsync(this WebSocket webSocket, Memory<byte> buffer, CancellationToken ct)
    {
        var result = await webSocket.ReceiveAsync(buffer, ct);
        if (result.MessageType == WebSocketMessageType.Close && webSocket.CloseStatus == WebSocketCloseStatus.PolicyViolation)
            throw new WebSocketClosedException(webSocket.CloseStatusDescription, webSocket.CloseStatus, null);

        int received = result.Count;
        while (!result.EndOfMessage && received < buffer.Length)
        {
            result = await webSocket.ReceiveAsync(buffer.Slice(received), ct);
            received += result.Count;
        }

        return (received, result);
    }
}
