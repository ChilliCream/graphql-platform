using System.Collections.Generic;

namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public sealed class RejectConnectionMessage
        : OperationMessage<IReadOnlyDictionary<string, object?>>
    {
        public RejectConnectionMessage(
            string message,
            IReadOnlyDictionary<string, object?>? extensions = null)
            : base(
                MessageTypes.Connection.Error,
                CreatePayload(message, extensions))
        {
        }

        private static IReadOnlyDictionary<string, object?> CreatePayload(
            string message,
            IEnumerable<KeyValuePair<string, object?>>? extensions)
        {
            Dictionary<string, object?> payload = extensions is null
                ? new Dictionary<string, object?>()
                : new Dictionary<string, object?>(extensions);
            payload[nameof(message)] = message;
            return payload;
        }
    }
}
