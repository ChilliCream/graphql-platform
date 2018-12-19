using System;

namespace HotChocolate.Resolvers
{
    public interface IResolverCache
    {
        bool TryGetResolver<T>(out T resolver);

        /// <summary>
        /// Tries to add the resolver to the cache.
        /// If there is alreade a resolver registered
        /// the registered resilver will be returned.
        /// </summary>
        /// <param name="resolverFactory">
        /// The factory to create a new resolver instance.
        /// </param>
        /// <param name="resolver">
        /// The new or resolved resolver instance.
        /// </param>
        /// <typeparam name="T">The resolver type.</typeparam>
        T AddOrGetResolver<T>(Func<T> resolverFactory);
    }

}
