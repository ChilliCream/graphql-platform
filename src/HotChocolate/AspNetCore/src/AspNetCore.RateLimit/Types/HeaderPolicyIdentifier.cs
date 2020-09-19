using System;
using HotChocolate.RateLimit;

namespace HotChocolate.AspNetCore.RateLimit
{
    internal class HeaderPolicyIdentifier : IPolicyIdentifier
    {
        public HeaderPolicyIdentifier(string header)
        {
            if (string.IsNullOrEmpty(header))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(header));
            }

            Header = header;
        }

        public string Header { get; }
    }
}
