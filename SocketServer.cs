using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Granite
{
    public class SocketServer<T> where T : SocketConnectionHandler
    {
        private IPEndPoint Endpoint;
        private IGraniteLogger Logger;
        private Socket AcceptorSocket;
        private SocketAsyncEventArgs AcceptorEventArg;

        private Dictionary<int, SocketConnection> Connections = new Dictionary<int, SocketConnection>();
        private int NextConnectionId;

        public SocketServer(IGraniteLogger logger)
        {
            Logger = logger;
        }

        public void Start(string address, int port)
        {
            var ip = GraniteUtil.StringToIpAddress(address);
            Endpoint = new IPEndPoint(ip, port);

            AcceptorEventArg = new SocketAsyncEventArgs();
            AcceptorEventArg.Completed += OnAsyncCompleted;

            AcceptorSocket = new Socket(Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            AcceptorSocket.NoDelay = true;
            AcceptorSocket.Blocking = false;
            AcceptorSocket.ExclusiveAddressUse = true;
            AcceptorSocket.LingerState = new LingerOption(false, 0);
            AcceptorSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);

            AcceptorSocket.Bind(Endpoint);
            // Refresh the endpoint property based on the actual endpoint created
            Endpoint = (IPEndPoint)AcceptorSocket.LocalEndPoint;

            AcceptorSocket.Listen(256);

            StartAccept(AcceptorEventArg);

        }

        public void Stop()
        {
            AcceptorEventArg.Completed -= OnAsyncCompleted;

            try
            {
                AcceptorSocket.Close();
                AcceptorSocket.Dispose();
                AcceptorEventArg.Dispose();

            }
            catch (ObjectDisposedException) { }


            CloseAllConnections();
            Logger.LogInformation("SocketServer stopped");
        }

        public void CloseAllConnections()
        {
            foreach (var connection in Connections.Values.ToList())
            {
                connection.Close();
            }
        }

        private void OnAsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void StartAccept(SocketAsyncEventArgs e)
        {
            // Socket must be cleared since the context object is being reused
            e.AcceptSocket = null;

            if (!AcceptorSocket.AcceptAsync(e))
            {
                ProcessAccept(e);
            }

        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            SocketError socketError = e.SocketError;

            if (socketError == SocketError.Success)
            {
                T connectionHandler = Activator.CreateInstance<T>();
                SocketConnection connection = new SocketConnection(NextConnectionId, e.AcceptSocket, Logger, connectionHandler);
                connection.OnClosed = OnSocketClosed;
                Connections[NextConnectionId] = connection;
                NextConnectionId++;
            }
            else
            {
                Logger.LogWarning("Accept failed {0}", socketError);
            }

            StartAccept(e);
        }

        private void OnSocketClosed(int id)
        {
            Connections.Remove(id);
        }

    }
}
