using System;
using static HotChocolate.Caching.Http.Properties.HttpQueryCacheResources;

namespace HotChocolate.Caching.Http;

internal static class ThrowHelper
{
    public static Exception UnexpectedCacheControlScopeValue(CacheControlScope value)
        => new(string.Format(ThrowHelper_UnexpectedCacheControlScopeValue, value));
}
