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
            using (Stream messageStream = message.CreateMessageStream())
            {
                await context.SendMessageAsync(
                    messageStream,
                    cancellationToken);
            }
        }

        private static Stream CreateMessageStream(this OperationMessage message)
        {
            string json = JsonConvert.SerializeObject(message, _settings);
            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }

        public static async Task<GenericOperationMessage> ReceiveMessageAsync(
            this IWebSocketContext context,
            CancellationToken cancellationToken)
        {
            using (var messageStream = new MemoryStream())
            {
                await context.ReceiveMessageAsync(
                    messageStream,
                    cancellationToken);

                string json = Encoding.UTF8.GetString(messageStream.ToArray());
                if (string.IsNullOrEmpty(json?.Trim()))
                {
                    return null;
                }

                return JsonConvert
                    .DeserializeObject<GenericOperationMessage>(json);
            }
        }
    }
}
