using System;
using static HotChocolate.Caching.Properties.CacheControlResources;

namespace HotChocolate.Caching;

internal static class ThrowHelper
{
    public static EncounteredIntrospectionFieldException EncounteredIntrospectionField()
        => new();
}

internal class EncounteredIntrospectionFieldException : Exception
{
    public EncounteredIntrospectionFieldException()
        : base(ThrowHelper_EncounteredIntrospectionField)
    {
    }
}
