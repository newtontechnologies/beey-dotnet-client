using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeeyApi.WebSockets
{
    class OpenedWebSocket : IDisposable
    {
        private Logging.ILog logger = Logging.LogProvider.For<OpenedWebSocket>();

        private ClientWebSocket webSocket;

        public OpenedWebSocket(ClientWebSocket openedWebSocket)
        {
            this.webSocket = openedWebSocket;
        }

        public async Task SendAsync(byte[] input, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            await SendAsync(new ArraySegment<byte>(input), messageType, endOfMessage, cancellationToken);
        }

        public async Task SendAsync(ArraySegment<byte> input, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            try
            {
                await webSocket.SendAsync(input, messageType, endOfMessage, cancellationToken);
            }
            catch (WebSocketException ex)
            {
                throw HandleException(ex);
            }
        }

        public async Task<byte[]> ReceiveAsync(int bufferSize, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[bufferSize > 0 ? bufferSize : WebSocketMessageBuilder.bufferSize];

            try
            {
                var response = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                byte[] result = new byte[response.Count];
                Array.Copy(buffer, result, response.Count);
                return result;
            }
            catch (WebSocketException ex)
            {
                throw HandleException(ex);
            }
        }

        public async Task CloseAsync(WebSocketCloseStatus closeStatus, string message, CancellationToken cancellationToken)
        {
            if (!webSocket.CloseStatus.HasValue)
            {
                await webSocket.CloseAsync(closeStatus, message, cancellationToken);
            }
        }

        public void Dispose()
        {
            webSocket.Dispose();
        }

        private Exception HandleException(WebSocketException ex)
        {
            if (webSocket.CloseStatus.HasValue)
            {
                return new WebSocketClosedException(ex, webSocket.CloseStatus, webSocket.CloseStatusDescription);
            }
            else
            {
                return ex;
            }
        }
    }
}
