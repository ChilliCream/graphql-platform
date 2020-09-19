using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HotChocolate.RateLimit
{
    public class LimitPolicy
    {
        public LimitPolicy(IList<IPolicyIdentifier> identifiers, TimeSpan period, int limit)
        {
            if (identifiers == null)
            {
                throw new ArgumentNullException(nameof(identifiers));
            }

            if (period < TimeSpan.FromSeconds(1))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(period), period, "Period cannot be less than 1 sec.");
            }

            if (limit < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(limit), limit, "Limit cannot be less than 1.");
            }

            Identifiers = new List<IPolicyIdentifier>(identifiers).AsReadOnly();
            Period = period;
            Limit = limit;
        }

        /// <summary>
        /// Policy identifiers
        /// </summary>
        public ReadOnlyCollection<IPolicyIdentifier> Identifiers { get; }

        /// <summary>
        /// Rate limit period
        /// </summary>
        public TimeSpan Period { get; }

        /// <summary>
        /// Maximum number of requests in a <see cref="Period"/>
        /// </summary>
        public int Limit { get; }
    }
}
