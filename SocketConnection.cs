using System;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Granite
{
    public class SocketConnection
    {
        public const int DefaultReceiveBufferSize = 4 * 1024 * 1024;

        public delegate void OnClosedEvent(int id);
        public OnClosedEvent OnClosed = delegate { };

        public int Id { get; private set; }

        private ReceiveBuffer ReceiveBuffer;
        private SocketAsyncEventArgs ReceiveEventArgs;
        private Socket Socket;
        private IGraniteLogger Logger;
        private SocketConnectionHandler ConnectionHandler;

        private ConcurrentQueue<SocketAsyncEventArgs> SendArgsQueue = new ConcurrentQueue<SocketAsyncEventArgs>();

        public SocketConnection(int id, Socket socket, IGraniteLogger logger, SocketConnectionHandler connectionHandler)
        {
            Id = id;
            Logger = logger;
            Socket = socket;
            ConnectionHandler = connectionHandler;

            Socket.NoDelay = true;
            Socket.Blocking = false;
            Socket.LingerState = new LingerOption(false, 0);


            ReceiveEventArgs = new SocketAsyncEventArgs();
            ReceiveBuffer = new ReceiveBuffer(connectionHandler.AllocateReceiveBuffer());
            ReceiveEventArgs.SetBuffer(ReceiveBuffer.Buffer, 0, ReceiveBuffer.Buffer.Length);
            ReceiveEventArgs.Completed += OnCompleted;


            ConnectionHandler.OnConnected(this);

            TryReceive();
        }

        public void Send(byte[] buffer, int length, object userToken = null)
        {
            if (Socket == null) return;

            if (!SendArgsQueue.TryDequeue(out var eventArgs))
            {
                eventArgs = new SocketAsyncEventArgs();
                eventArgs.Completed += OnCompleted;
            }
           
            try
            {
                eventArgs.SetBuffer(buffer, 0, length);
                eventArgs.UserToken = userToken;

                if (!Socket.SendAsync(eventArgs))
                {
                    ProcessSend(eventArgs);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("SocketConnection.Send {0} {1}", ex.Message, ex.StackTrace);
            }
           
        }

        public void Close()
        {
            if (Socket == null) return;

            ReceiveEventArgs.Dispose();

            while (SendArgsQueue.TryDequeue(out var sendArgs))
            {
                sendArgs.Dispose();
            }

            // close the socket associated with the client
            try
            {
                Socket.Shutdown(SocketShutdown.Send);
            }
            // throws if client process has already closed
            catch (Exception ex)
            {
                Logger.LogInformation("SocketConnection.Close {0} {1}", ex.Message, ex.StackTrace);
            }

            

            if (Socket != null)
            {
                Socket.Close();
                Socket = null;
            }
            
            Logger.LogDebug("SocketConnection {0} closed", Id);
            OnClosed(Id);
            ConnectionHandler.OnDisconnected();
        }

        private void TryReceive()
        {
            if (Socket == null) return;

            if (!Socket.ReceiveAsync(ReceiveEventArgs))
            {
                if (ProcessReceive(ReceiveEventArgs))
                {
                    TryReceive();
                }
            }
        }

        private void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                case SocketAsyncOperation.Receive:
                    if (ProcessReceive(e))
                    {
                        TryReceive();
                    }
                    break;
                default:
                    Logger.LogWarning("SocketConnection {0} {1}", Id, e.LastOperation);
                    Close();
                    break;
            }
        }

        private bool ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                try
                {
                    ReceiveBuffer.Write(e.BytesTransferred);
                    ConnectionHandler.OnReceive(ReceiveBuffer);
                    Logger.LogDebug("SocketConnection ProcessReceive {0} {1}", Id, e.BytesTransferred);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("SocketConnection.ProcessReceive {0} {1}", ex.Message, ex.StackTrace);
                }
                return true;
            }
            else
            {
                Close();
                return false;
            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                ConnectionHandler.SendCompleted(e.UserToken);
                SendArgsQueue.Enqueue(e);
                Logger.LogDebug("SocketConnection ProcessSend {0} {1}", Id, e.BytesTransferred);
            }
            else
            {
                Close();
            }
        }

    }
}
