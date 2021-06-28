using System.Text;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal static class MessageSerialization
    {
        private static readonly JsonSerializerSettings _jsonSettings =
            new()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };

        private static readonly UTF8Encoding _encoding = new();

        private static readonly byte[] _keepConnectionAliveMessage =
           SerializeInternal(KeepConnectionAliveMessage.Default);

        private static readonly byte[] _acceptConnectionMessage =
           SerializeInternal(AcceptConnectionMessage.Default);

        public static byte[] Serialize(this OperationMessage message)
        {
            if (message is KeepConnectionAliveMessage)
            {
                return _keepConnectionAliveMessage;
            }

            if (message is AcceptConnectionMessage)
            {
                return _acceptConnectionMessage;
            }

            return SerializeInternal(message);
        }

        private static byte[] SerializeInternal(OperationMessage message)
        {
            return _encoding.GetBytes(JsonConvert.SerializeObject(message, _jsonSettings));
        }
    }
}
