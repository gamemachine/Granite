# Granite
 .Net TCP server
 
 Minimal modern TCP server using SocketAsyncEventArgs and length prefixed messages. Tries to not get in your way and provide a minimal base to work from. 
 
 Some of the design was inspired by https://github.com/davidfowl/BedrockFramework.
 
 I didn't want to add more unnecessary abstractions so GraniteMessageType you will need to customize for your own message types.
 
 Create a class that inherits from SocketConnectionHandler.  Create a logging class that implements IGraniteLogger.  
 Instantiate the server with your connection handler.
 
 OnConnected hands you a SocketConnection.  You send messages from inside your connection handler using connection.Send.
 
 Send flow is write your header, then write the message, and send.
 
 OnReceive is called every time data is received. Call buffer.HasMessage while it's true. And make sure to call buffer.Consume which advances the internal pointer
 
 ```
 while (buffer.HasMessage(out GraniteMessageType messageType, out int messageLength))
  {
      int offset = buffer.Index;  // offset to message start
      int consumed = GraniteMessageHeader.HeaderLength + messageLength;
      buffer.Consume(consumed);
  }

 ```
 
 
