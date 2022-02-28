using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net.WebSockets;


namespace IqApiNetCore.Utilities
{
    public class CustomWebSocket
    {
        private ClientWebSocket webSocket = null;
        private CancellationToken cancelToken = new CancellationToken();
        public event EventHandler<EventArgs> OnMessageReceived;
        public int ReceiveBufferSize { get; set; } = 8192;
        public async Task ConnectAsync(string uri)
        {
            try
            {
                webSocket = new ClientWebSocket();
                await webSocket.ConnectAsync(new Uri(uri), cancelToken);

                Task.Factory.StartNew(() =>
                {
                    ConnLoop();
                });
            }
            catch
            {

            }
        }


        private async void ConnLoop()
        {
            MemoryStream outputStream = new MemoryStream(ReceiveBufferSize);

            WebSocketReceiveResult result = null;
            while (webSocket.State == WebSocketState.Open && !cancelToken.IsCancellationRequested)
            {
                byte[] buffer = new byte[ReceiveBufferSize];
                outputStream = new MemoryStream(ReceiveBufferSize);
                do
                {
                    result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);

                    if (result != null && result.MessageType != WebSocketMessageType.Close)
                    {
                        outputStream.Write(buffer, 0, result.Count);
                    }
                }
                while (!result.EndOfMessage);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;
                outputStream.Position = 0;
                using (MemoryStream ms = new MemoryStream())
                {
                    outputStream.CopyTo(ms);
                    byte[] data = ms.ToArray();
                    string msg = Encoding.UTF8.GetString(data, 0, data.Length);

                    Task.Factory.StartNew(() =>
                    {
                        OnMessageReceived(msg, new EventArgs());
                    });
                }
                //OnMessageReceived()
            }

        }
        public async void SendMessageAsync(string msg)
        {
            try

            {
                await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch
            {
                //this.DisconnectAsync().Wait();
            }
        }
        public async Task DisconnectAsync()
        {
            if (webSocket is null) return;
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseOutputAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            webSocket.Dispose();
            webSocket = null;
        }
    }
}
