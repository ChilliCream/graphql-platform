#if !ASPNETCLASSIC

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
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

        public static Task SendConnectionErrorMessageAsync(
            this IWebSocketContext context,
            IReadOnlyDictionary<string, object> payload,
            CancellationToken cancellationToken)
        {
            return SendMessageAsync(
                context,
                new DictionaryOperationMessage
                {
                    Type = MessageTypes.Connection.Error,
                    Payload = payload
                },
                cancellationToken);
        }

        public static Task SendSubscriptionDataMessageAsync(
            this IWebSocketContext context,
            string id,
            IReadOnlyQueryResult result,
            CancellationToken cancellationToken)
        {
            return SendMessageAsync(
                context,
                new DataOperationMessage
                {
                    Type = MessageTypes.Subscription.Data,
                    Id = id,
                    Payload = ToDictionary(result)
                },
                cancellationToken);
        }

        private static IReadOnlyDictionary<string, object> ToDictionary(
            IReadOnlyQueryResult result)
        {
            var internalResult = new Dictionary<string, object>();

            if ( result.Errors.Count > 0)
            {
                internalResult["errors"] = result.Errors;
            }

            if (result.Data.Count > 0)
            {
                internalResult["data"] = result.Data;
            }

            if (result.Extensions.Count > 0)
            {
                internalResult["extensions"] = result.Extensions;
            }

            return internalResult;
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
                    cancellationToken)
                    .ConfigureAwait(false);
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
                    cancellationToken)
                    .ConfigureAwait(false);

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

#endif
