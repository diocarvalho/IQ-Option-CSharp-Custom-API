using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
namespace IqApiNetCore
{
    public class WebSocketClient : IDisposable
    {
        public event EventHandler<EventArgs> OnMessageReceived;      
        public int ReceiveBufferSize { get; set; } = 8192;

        private ClientWebSocket clientWebSocket;
        private CancellationTokenSource cTokenSource;

        public async Task ConnectAsync(string url)
        {
            if(clientWebSocket != null)
            {
                if(clientWebSocket.State == WebSocketState.Open)
                {
                    return;
                }else
                {
                    clientWebSocket.Dispose();
                }
            }
            if (cTokenSource != null)
                cTokenSource.Dispose();

            clientWebSocket = new ClientWebSocket();
            cTokenSource = new CancellationTokenSource();
            await clientWebSocket.ConnectAsync(new Uri(url), cTokenSource.Token);

            await Task.Factory.StartNew(ConnectionLoop, cTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public async Task DisconnectAsync()
        {
            if (clientWebSocket is null) return;
            if (clientWebSocket.State == WebSocketState.Open)
            {
                cTokenSource.CancelAfter(TimeSpan.FromSeconds(5));
                await clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
                await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            clientWebSocket.Dispose();
            cTokenSource.Dispose();
            clientWebSocket = null;
            cTokenSource = null;
        }

        private async Task ConnectionLoop()
        {
            var loopToken = cTokenSource.Token;
            MemoryStream outputStream = null;
            WebSocketReceiveResult receiveResult;
            var buffer = new byte[ReceiveBufferSize];
            try
            {
                while (!loopToken.IsCancellationRequested)
                {
                    outputStream = new MemoryStream(ReceiveBufferSize);
                    do
                    {
                        receiveResult = await clientWebSocket.ReceiveAsync(buffer, cTokenSource.Token);
                        if (receiveResult.MessageType != WebSocketMessageType.Close)
                        {
                            outputStream.Write(buffer, 0, receiveResult.Count);
                        }
                    }
                    while (!receiveResult.EndOfMessage);
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                        break;
                    outputStream.Position = 0;
                    string msg = ResponseReceived(outputStream);
                    OnMessageReceived(msg, new EventArgs());
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                outputStream.Dispose();
            }
        }

        public void SendMessageAsync(string msg)
        {
            clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
        }

        private string ResponseReceived(Stream inStream)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                inStream.CopyTo(ms);
                byte[] data = ms.ToArray();
                return Encoding.UTF8.GetString(data, 0, data.Length);
            }            
        }

        public void Dispose() => DisconnectAsync().Wait();


    }
}
