using System;

namespace HotChocolate.RateLimit
{
    public class LimitDirective
    {
        public LimitDirective(string policy)
        {
            if (string.IsNullOrEmpty(policy))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(policy));
            }

            Policy = policy;
        }

        public string Policy { get; }
    }
}
