using System;
using System.Collections.Generic;

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    public class ConnectionStatus
    {
        private ConnectionStatus(
            bool accepted,
            IReadOnlyDictionary<string, object> response)
        {
            Accepted = accepted;
            Response = response;
        }

        public bool Accepted { get; }

        public IReadOnlyDictionary<string, object> Response { get; }

        public static ConnectionStatus Accept() =>
            new ConnectionStatus(true, null);

        public static ConnectionStatus Reject(string messsage)
        {
            if (string.IsNullOrEmpty(messsage))
            {
                throw new ArgumentException(
                    "Message cannot be null or empty.",
                    nameof(messsage));
            }

            var response = new Dictionary<string, object>
            {
                { "message", messsage }
            };

            return Reject(response);
        }

        public static ConnectionStatus Reject() =>
            Reject(default(IReadOnlyDictionary<string, object>));

        public static ConnectionStatus Reject(
            IReadOnlyDictionary<string, object> response) =>
                new ConnectionStatus(false, response);
    }
}
