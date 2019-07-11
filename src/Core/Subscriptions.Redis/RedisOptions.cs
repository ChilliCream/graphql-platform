using System;
using System.Collections.Generic;

namespace HotChocolate.Subscriptions
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
    }
}
