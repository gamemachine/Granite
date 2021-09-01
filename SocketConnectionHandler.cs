namespace Granite
{
    public class SocketConnectionHandler
    {
        
        public virtual byte[] AllocateReceiveBuffer()
        {
            return new byte[SocketConnection.DefaultReceiveBufferSize];
        }

        public virtual void OnConnected(SocketConnection connection)
        {

        }

        public virtual void OnDisconnected()
        {

        }

        public virtual void OnReceive(ReceiveBuffer buffer)
        {

        }

        public virtual void SendCompleted(object userToken)
        {

        }
    }
}
