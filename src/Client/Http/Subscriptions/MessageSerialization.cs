using System;
using System.Text;
using System.Text.Json;
using StrawberryShake.Http.Subscriptions.Messages;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Http.Subscriptions
{
    internal static class MessageSerialization
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IgnoreNullValues = true,
            IgnoreReadOnlyProperties = false
        };

        private static readonly byte[] _keepConnectionAliveMessage =
            JsonSerializer.SerializeToUtf8Bytes(KeepConnectionAliveMessage.Default, _options);

        private static readonly byte[] _acceptConnectionMessage =
            JsonSerializer.SerializeToUtf8Bytes(AcceptConnectionMessage.Default, _options);

        public static ReadOnlyMemory<byte> Serialize(
            this OperationMessage message)
        {
            if (message is KeepConnectionAliveMessage)
            {
                return _keepConnectionAliveMessage;
            }

            if (message is AcceptConnectionMessage)
            {
                return _acceptConnectionMessage;
            }

            return JsonSerializer.SerializeToUtf8Bytes(message, _options);
        }
    }
}
