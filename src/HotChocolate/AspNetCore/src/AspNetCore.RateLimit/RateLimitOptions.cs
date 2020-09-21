using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HotChocolate.RateLimit;

namespace HotChocolate.AspNetCore.RateLimit
{
    public class RateLimitOptions
    {
        private readonly IDictionary<string, LimitPolicy> _policies =
            new Dictionary<string, LimitPolicy>(StringComparer.InvariantCultureIgnoreCase);

        public IReadOnlyDictionary<string, LimitPolicy> Policies =>
            new ReadOnlyDictionary<string, LimitPolicy>(_policies);

        public void AddPolicy(string name, Action<RateLimitPolicyBuilder> configure)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(name));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var policyBuilder = new RateLimitPolicyBuilder();
            configure(policyBuilder);
            _policies[name] = policyBuilder.Build();
        }
    }
}
