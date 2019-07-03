using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api
{
    public static class Helpers
    {
        /// <summary>
        /// fill buffer with entire message (from start)
        /// </summary>
        /// <param name="webSocket"></param>
        /// <param name="buffer"></param>
        /// <param name="ct"></param>
        /// <returns>bytes written to buffer and last processed message result, when !EndOfMessage buffer is filled before end of message was received</returns>
        public async static Task<(int bytes, ValueWebSocketReceiveResult lastResult)> ReceiveMessageAsync(this WebSocket webSocket, Memory<byte> buffer, CancellationToken ct)
        {
            var result = await webSocket.ReceiveAsync(buffer, ct);
            int received = result.Count;
            while (!result.EndOfMessage && received < buffer.Length)
            {
                result = await webSocket.ReceiveAsync(buffer.Slice(received), ct);
                received += result.Count;
            }
            return (received, result);
        }
    }
}
