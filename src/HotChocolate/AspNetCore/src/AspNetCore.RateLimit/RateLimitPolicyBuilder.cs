using System;
using System.Collections.Generic;
using HotChocolate.RateLimit;

namespace HotChocolate.AspNetCore.RateLimit
{
    public class RateLimitPolicyBuilder
    {
        private readonly List<IPolicyIdentifier> _identifiers = new List<IPolicyIdentifier>();
        private TimeSpan _period;
        private int _limit;

        public RateLimitPolicyBuilder AddClaimIdentifier(string claimType)
        {
            _identifiers.Add(new ClaimsPolicyIdentifier(claimType));
            return this;
        }

        public RateLimitPolicyBuilder AddHeaderIdentifier(string header)
        {
            var headerPolicyIdentifier = new HeaderPolicyIdentifier(header);
            _identifiers.Add(headerPolicyIdentifier);
            return this;
        }

        public void WithLimit(TimeSpan period, int limit)
        {
            _period = period;
            _limit = limit;
        }

        public LimitPolicy Build()
        {
            return new LimitPolicy(_identifiers, _period, _limit);
        }
    }
}
