using System;
using HotChocolate.Resolvers;

namespace HotChocolate.Caching;

public static class ResolverContextExtensions
{
    public static IResolverContext CacheControl(
        this IResolverContext context,
        int? maxAge = null, CacheControlScope? scope = null)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // todo: use arguments

        return context;
    }
}