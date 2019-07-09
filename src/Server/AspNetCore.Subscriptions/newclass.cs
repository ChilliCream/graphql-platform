using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public class SocketConnectionMessageExtensions
    {







    }

    public interface IConnectMessageInterceptor
    {
        Task<ConnectionStatus> OnReceiveAsync(
            ISocketConnection connection,
            InitializeConnectionMessage message,
            CancellationToken cancellationToken);
    }



    internal static class MessageSerialization
    {
        public static readonly JsonSerializerSettings JsonSettings =
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };

        public static readonly UTF8Encoding Encoding = new UTF8Encoding();

        private static readonly byte[] _keepConnectionAliveMessage =
           SerializeInternal(KeepConnectionAliveMessage.Default);

        private static readonly byte[] _acceptConnectionMessage =
           SerializeInternal(AcceptConnectionMessage.Default);

        public static ReadOnlySpan<byte> Serialize(
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

            return SerializeInternal(message);
        }

        private static byte[] SerializeInternal(OperationMessage message)
        {
            return Encoding.GetBytes(
                JsonConvert.SerializeObject(message, JsonSettings));
        }
    }


}
