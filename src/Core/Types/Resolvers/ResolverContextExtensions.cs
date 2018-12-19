using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers
{
    public static class ResolverContextExtensions
    {
        public static T DataLoader<T>(this IResolverContext resolverContext)
        {
            return resolverContext.DataLoader<T>(typeof(T).FullName);
        }
    }
}
