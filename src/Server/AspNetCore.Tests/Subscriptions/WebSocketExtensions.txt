using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
                new GenericOperationMessage
                {
                    Type = MessageTypes.Connection.Initialize
                });
        }

        public static async Task<string> SendSubscriptionStartAsync(
            this WebSocket webSocket, SubscriptionQuery query)
        {
            string id = Guid.NewGuid().ToString("N");

            await SendMessageAsync(
               webSocket,
               new StartOperationMessage
               {
                   Type = MessageTypes.Subscription.Start,
                   Id = id,
                   Payload = query
               });

            return id;
        }

        public static async Task SendMessageAsync(
            this WebSocket webSocket,
            OperationMessage message)
        {
            var buffer = new byte[_maxMessageSize];

            using (Stream stream = message.CreateMessageStream())
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

        private static Stream CreateMessageStream(this OperationMessage message)
        {
            string json = JsonConvert.SerializeObject(message, _settings);

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }

        public static async Task<GenericOperationMessage> ReceiveServerMessageAsync(
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
