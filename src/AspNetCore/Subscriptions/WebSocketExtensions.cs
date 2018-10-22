using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public static class WebSocketExtensions
    {
        private const int _maxMessageSize = 1024 * 4;

        private static readonly JsonSerializerSettings _settings =
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };

        public static Task SendConnectionAcceptMessageAsync(
            this IWebSocketContext context,
            CancellationToken cancellationToken)
        {
            return SendMessageAsync(
                context,
                new GenericOperationMessage
                {
                    Type = MessageTypes.Connection.Accept
                },
                cancellationToken);
        }

        public static Task SendConnectionKeepAliveMessageAsync(
            this IWebSocketContext context,
            CancellationToken cancellationToken)
        {
            return SendMessageAsync(
                context,
                new GenericOperationMessage
                {
                    Type = MessageTypes.Connection.KeepAlive
                },
                cancellationToken);
        }

        public static Task SendSubscriptionDataMessageAsync(
            this IWebSocketContext context,
            string id,
            IQueryExecutionResult result,
            CancellationToken cancellationToken)
        {
            return SendMessageAsync(
                context,
                new DataOperationMessage
                {
                    Type = MessageTypes.Subscription.Data,
                    Id = id,
                    Payload = result.ToDictionary()
                },
                cancellationToken);
        }

        public static Task SendSubscriptionCompleteMessageAsync(
            this IWebSocketContext context,
            string id,
            CancellationToken cancellationToken)
        {
            return SendMessageAsync(
                context,
                new GenericOperationMessage
                {
                    Type = MessageTypes.Subscription.Complete,
                    Id = id
                },
                cancellationToken);
        }

        public static async Task SendMessageAsync(
            this IWebSocketContext context,
            OperationMessage message,
            CancellationToken cancellationToken)
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
                        isEndOfMessage, cancellationToken);
                } while (read == _maxMessageSize);
            }
        }

        private static Stream CreateMessageStream(this OperationMessage message)
        {
            string json = JsonConvert.SerializeObject(message, _settings);
            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }

        public static Task<GenericOperationMessage> ReceiveMessageAsync(
            this IWebSocketContext context,
            CancellationToken cancellationToken)
        {
            return ReceiveMessageAsync(context.WebSocket, cancellationToken);
        }

        public static async Task<GenericOperationMessage> ReceiveMessageAsync(
            this WebSocket webSocket,
            CancellationToken cancellationToken)
        {
            using (var stream = new MemoryStream())
            {
                WebSocketReceiveResult result;
                var buffer = new byte[_maxMessageSize];

                do
                {
                    result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationToken);
                    stream.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                string json = Encoding.UTF8.GetString(stream.ToArray());
                if (string.IsNullOrEmpty(json?.Trim()))
                {
                    return null;
                }

                return JsonConvert.DeserializeObject<GenericOperationMessage>(json);
            }
        }
    }






}
