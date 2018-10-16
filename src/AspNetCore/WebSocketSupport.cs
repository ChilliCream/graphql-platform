using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HotChocolate.AspNetCore
{
    public class OperationMessage
    {
        public string Id { get; set; }

        public string Type { get; set; }

        public JObject Payload { get; set; }
    }


    /*
    export interface OperationMessage {
  payload?: any;
  id?: string;
  type: string;
}

     */



    public class WebSocketConnectionContext

    {
        private ConcurrentDictionary<string, ISubscription> _subscriptions =
            new ConcurrentDictionary<string, ISubscription>();

        public void RegisterSubscription(ISubscription subscription)
        {

        }

        public void UnregisterSubscription(string subscriptionId)
        {

        }
    }

    public interface IWebSocketContext
        : IDisposable
    {
        WebSocket WebSocket { get; }

        void RegisterSubscription(ISubscription subscription);
        void UnregisterSubscription(string subscriptionId);
    }


    public class WebSocketConnection
        : IDisposable
    {
        private const string _protocol = "graphql-ws";

        private static IRequestHandler[] _requestHandlers =
            new IRequestHandler[]
            {
                new ConnectionInitializeHandler()
            };
        private static IRequestHandler _unknownRequestHandler;

        private readonly IWebSocketMessageReceiver _receiver;
        private readonly IWebSocketContext _context;

        private WebSocketConnection(
            IWebSocketMessageReceiver receiver,
            IWebSocketContext context)
        {
            _receiver = receiver;
            _context = context;

            _receiver.OnReceive(OnReceiveMessage);
        }

        private Task OnReceiveMessage(
            IWebSocketContext context,
            OperationMessage message)
        {
            foreach (IRequestHandler requestHandler in _requestHandlers)
            {
                if (requestHandler.CanHandle(message))
                {
                    return requestHandler.HandleAsync(context, message);
                }
            }

            return _unknownRequestHandler.HandleAsync(context, message);
        }

        // TODO : Keep Alive
        // TODO : Hanlde close status

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public static async Task<WebSocketConnection> TryCreate(HttpContext context)
        {
            WebSocket socket = await context.WebSockets.AcceptWebSocketAsync(_protocol);
            throw new NotImplementedException();
        }
    }

    public interface IWebSocketMessageReceiver
        : IDisposable
    {
        void OnReceive(ReceiveMessage receiveMessage);
    }

    public delegate Task ReceiveMessage(
        IWebSocketContext context,
        OperationMessage message);

    public interface IRequestHandler
    {
        Task HandleAsync(IWebSocketContext context, OperationMessage message);

        bool CanHandle(OperationMessage message);
    }

    public static class WebSocketExtensions
    {
        private const int _maxMessageSize = 1024 * 4;

        public static Task SendConnectionAcceptMessageAsync(
            this IWebSocketContext context)
        {
            return SendMessageAsync(
                context,
                new OperationMessage
                {
                    Type = MessageTypes.Connection.Accept
                });
        }

        public static Task SendConnectionKeepAliveMessageAsync(
            this IWebSocketContext context)
        {
            return SendMessageAsync(
                context,
                new OperationMessage
                {
                    Type = MessageTypes.Connection.KeepAlive
                });
        }

        public static async Task SendMessageAsync(
            this IWebSocketContext context,
            OperationMessage message)
        {
            var buffer = new byte[_maxMessageSize];

            using (Stream stream = message.CreateMessageStream())
            {
                int read = 0;
                do
                {
                    read = stream.Read(buffer, 0, buffer.Length);
                    var segment = new ArraySegment<byte>(buffer, 0, read);
                    bool isEndOfMessage = stream.Position == stream.Length;

                    await context.WebSocket.SendAsync(
                        segment, WebSocketMessageType.Text,
                        isEndOfMessage, CancellationToken.None);
                } while (read == _maxMessageSize);
            }
        }
        private static Stream CreateMessageStream(this OperationMessage message)
        {
            return new MemoryStream(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(message)));
        }
    }



    /// <summary>
    /// The server may responses with this message to the
    /// GQL_CONNECTION_INIT from client, indicates the
    /// server accepted the connection.
    /// </summary>
    public sealed class ConnectionInitializeHandler
        : IRequestHandler
    {
        public bool CanHandle(OperationMessage message)
        {
            return message.Type == MessageTypes.Connection.Initialize;
        }

        public async Task HandleAsync(
            IWebSocketContext context,
            OperationMessage message)
        {
            await context.SendConnectionAcceptMessageAsync();
            await context.SendConnectionKeepAliveMessageAsync();
        }
    }





    public sealed class ConnectionInitializeMessage
        : OperationMessage
    {
        public JObject Payload { get; set; }
    }

    public interface ISubscription
        : IDisposable
    {

    }


    internal static class MessageTypes
    {
        internal static class Connection
        {
            public static readonly string Initialize = "connection_init";
            public static readonly string Accept = "connection_ack";
            public static readonly string Error = "connection_error";
            public static readonly string KeepAlive = "ka";
            public static readonly string Terminate = "connection_terminate";
        }

        public static class Subscription
        {
            public static readonly string Start = "start";
            public static readonly string Data = "data";
            public static readonly string Error = "error";
            public static readonly string Complete = "complete";
            public static readonly string Stop = "stop";
        }
    }






}
