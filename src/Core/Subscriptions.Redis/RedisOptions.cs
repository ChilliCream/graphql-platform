using System;
using System.Collections.Generic;

namespace HotChocolate.Subscriptions.Redis
{
    /// <summary>
    /// Defines Redis server configuration
    /// </summary>
    public class RedisOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisOptions"/> class.
        /// </summary>
        /// <param name="endpoints"></param>
        public RedisOptions(IList<string> endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (endpoints.Count < 1)
            {
                throw new ArgumentException(
                    "Must contain at least one endpoint",
                    nameof(endpoints));
            }

            Endpoints = endpoints;
        }

        /// <summary>
        /// List of Redis endpoints including port.
        /// e.g. "localhost:6379"
        /// </summary>
        public IList<string> Endpoints { get; }

        /// <summary>
        /// Identification for the connection within redis
        /// </summary>
        public string ClientName { get; set; }

        /// <summary>
        /// Password for the redis server
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Creates a <see cref="RedisOptions"/> from a single endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        public static implicit operator RedisOptions(string endpoint)
            => new RedisOptions(new[] { endpoint });
    }
}
