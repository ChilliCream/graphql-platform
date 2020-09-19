using System;
using HotChocolate.RateLimit;

namespace HotChocolate.AspNetCore.RateLimit
{
    public class ClaimsPolicyIdentifier : IPolicyIdentifier
    {
        public ClaimsPolicyIdentifier(string claimType)
        {
            if (string.IsNullOrEmpty(claimType))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(claimType));
            }

            ClaimType = claimType;
        }

        public string ClaimType { get; }
    }
}
