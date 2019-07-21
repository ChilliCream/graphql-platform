using System.Linq;
using System.Collections.Generic;

namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public sealed class RejectConnectionMessage
        : OperationMessage<IReadOnlyDictionary<string, object>>
    {
        public RejectConnectionMessage(
            string message)
            : base(
                MessageTypes.Connection.Error,
                new Dictionary<string, object> { { nameof(message), message } })
        {
        }

        public RejectConnectionMessage(
            string message, IReadOnlyDictionary<string, object> extensions)
            : base(
                MessageTypes.Connection.Error,
                CreatePayload(message, extensions))
        {
        }

        private static IReadOnlyDictionary<string, object> CreatePayload(
            string message,
            IEnumerable<KeyValuePair<string, object>> extensions)
        {
            var payload = extensions.ToDictionary(t => t.Key, t => t.Value);
            payload[nameof(message)] = message;
            return payload;
        }
    }
}
