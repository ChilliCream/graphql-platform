using System;

#nullable enable

namespace HotChocolate.Types
{
    public sealed class SpecifiedByDirective
    {
        public SpecifiedByDirective(Uri url)
        {
            Url = url;
        }

        public Uri Url { get; }
    }
}
