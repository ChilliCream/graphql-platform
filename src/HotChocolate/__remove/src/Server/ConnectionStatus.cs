using System;
using System.Collections.Generic;

namespace HotChocolate.Server
{
    public class ConnectionStatus
    {
        private ConnectionStatus(
            bool accepted,
            string message,
            IReadOnlyDictionary<string, object> extensions)
        {
            Accepted = accepted;
            Message = message;
            Extensions = extensions;
        }

        public bool Accepted { get; }

        public string Message { get; }

        public IReadOnlyDictionary<string, object> Extensions { get; }

        public static ConnectionStatus Accept() =>
            new ConnectionStatus(true, null, null);

        public static ConnectionStatus Reject(
            string message,
            IReadOnlyDictionary<string, object> extensions)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    "Message cannot be null or empty.",
                    nameof(message));
            }

            return new ConnectionStatus(false, message, extensions);
        }

        public static ConnectionStatus Reject(string message) =>
            Reject(message, null);

        public static ConnectionStatus Reject() =>
            Reject("Your connection was rejected.", null);

        public static ConnectionStatus Reject(
            IReadOnlyDictionary<string, object> extensions) =>
                Reject("Your connection was rejected.", extensions);
    }
}
