using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.Language;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal static class WebSocketExtensions
    {
        private const int _maxMessageSize = 1024 * 4;

        private static readonly JsonSerializerSettings _settings =
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };

        public static Task SendConnectionInitializeAsync(
            this WebSocket webSocket)
        {
            return SendMessageAsync(
                webSocket,
                new InitializeConnectionMessage(null));
        }

        public static Task SendTerminateConnectionAsync(
            this WebSocket webSocket)
        {
            return SendMessageAsync(
                webSocket,
                TerminateConnectionMessage.Default);
        }

        public static async Task SendSubscriptionStartAsync(
            this WebSocket webSocket,
            string subscriptionId,
            GraphQLRequest request,
            bool largeMessage = false)
        {
            await SendMessageAsync(
               webSocket,
               new DataStartMessage(subscriptionId, request),
               largeMessage);
        }

        public static async Task SendSubscriptionStopAsync(
            this WebSocket webSocket,
            string subscriptionId)
        {
            await SendMessageAsync(
               webSocket,
               new DataStopMessage(subscriptionId));
        }

        public static async Task SendEmptyMessageAsync(
            this WebSocket webSocket)
        {
            var buffer = new byte[1];

            var segment = new ArraySegment<byte>(buffer, 0, 0);

            await webSocket.SendAsync(
                segment, WebSocketMessageType.Text,
                true, CancellationToken.None);
        }

        public static async Task SendMessageAsync(
            this WebSocket webSocket,
            OperationMessage message,
            bool largeMessage = false)
        {
            var buffer = new byte[_maxMessageSize];

            using (Stream stream = message.CreateMessageStream(largeMessage))
            {
                var read = 0;

                do
                {
                    read = stream.Read(buffer, 0, buffer.Length);
                    var segment = new ArraySegment<byte>(buffer, 0, read);
                    var isEndOfMessage = stream.Position == stream.Length;

                    await webSocket.SendAsync(
                        segment, WebSocketMessageType.Text,
                        isEndOfMessage, CancellationToken.None);
                } while (read == _maxMessageSize);
            }
        }

        private static Stream CreateMessageStream(
            this OperationMessage message,
            bool largeMessage)
        {
            if (message is DataStartMessage dataStart)
            {
                string query = QuerySyntaxSerializer.Serialize(
                    dataStart.Payload.Query);

                var payload = new Dictionary<string, object>
                {
                    { "query", query },
                };

                if (dataStart.Payload.QueryName != null)
                {
                    payload["namedQuery"] = dataStart.Payload.QueryName;
                }

                if (dataStart.Payload.OperationName != null)
                {
                    payload["operationName"] = dataStart.Payload.OperationName;
                }

                if (dataStart.Payload.Variables != null)
                {
                    payload["variables"] = dataStart.Payload.Variables;
                }

                message = new HelperOperationMessage(
                    dataStart.Type, dataStart.Id, payload);
            }

            string json = JsonConvert.SerializeObject(message, _settings);
            if (largeMessage)
            {
                json += new string(' ', 1024 * 16);
            }
            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }

        public static async Task<IReadOnlyDictionary<string, object>>
            ReceiveServerMessageAsync(
                this WebSocket webSocket)
        {
            using (var stream = new MemoryStream())
            {
                WebSocketReceiveResult result;
                var buffer = new byte[_maxMessageSize];

                do
                {
                    result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None);
                    stream.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                return (IReadOnlyDictionary<string, object>)
                    Utf8GraphQLRequestParser.ParseJson(stream.ToArray());
            }
        }

        private class HelperOperationMessage
            : OperationMessage
        {
            public HelperOperationMessage(
                string type, string id, object payload)
                : base(type, id)
            {
                Payload = payload;
            }

            public object Payload { get; }
        }
    }
}
