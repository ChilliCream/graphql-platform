using System;

#nullable enable

namespace HotChocolate.Types
{
    [Serializable]
    public sealed class DeprecatedDirective
    {
        public DeprecatedDirective()
        {
        }

        public DeprecatedDirective(string? reason)
        {
            Reason = reason;
        }

        public string? Reason { get; }
    }
}
