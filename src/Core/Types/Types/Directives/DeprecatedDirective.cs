using System;

namespace HotChocolate.Types
{
    [Serializable]
    public sealed class DeprecatedDirective
    {
        public DeprecatedDirective()
        {
            Reason = WellKnownDirectives.DeprecationDefaultReason;
        }

        public DeprecatedDirective(string reason)
        {
            // TODO : resources
            if (string.IsNullOrEmpty(reason))
            {
                throw new ArgumentException(
                    "The deprecation reason cannot be null or empty.",
                    nameof(reason));
            }

            Reason = reason;
        }

        public string Reason { get; }
    }
}
