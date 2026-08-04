using static HotChocolate.Caching.Properties.CacheControlCoreResources;

namespace HotChocolate.Caching;

internal static class ThrowHelper
{
    public static NotSupportedException UnexpectedCacheControlScopeValue(CacheControlScope value)
        => new(string.Format(ThrowHelper_UnexpectedCacheControlScopeValue, value));
}
