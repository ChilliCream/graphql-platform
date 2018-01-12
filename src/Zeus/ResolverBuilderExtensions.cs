using System;
using System.Threading.Tasks;

namespace Zeus
{
    public static class ResolverBuilderExtensions
    {
        public static IResolverBuilder Add(this IResolverBuilder resolverBuilder,
            string typeName, Func<IResolverContext, Task<object>> resolver)
        {
            return resolverBuilder.Add(typeName, (context, cancellationToken) => resolver(context));
        }

        public static IResolverBuilder Add(this IResolverBuilder resolverBuilder,
           string typeName, string fieldName, Func<IResolverContext, Task<object>> resolver)
        {
            return resolverBuilder.Add(typeName, fieldName, (context, cancellationToken) => resolver(context));
        }
    }


}
