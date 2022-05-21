using System;
using static HotChocolate.Caching.Properties.CacheControlResources;

namespace HotChocolate.Caching;

internal static class ThrowHelper
{
    public static Exception EncounteredIntrospectionField()
        => new(ThrowHelper_EncounteredIntrospectionField);
}
