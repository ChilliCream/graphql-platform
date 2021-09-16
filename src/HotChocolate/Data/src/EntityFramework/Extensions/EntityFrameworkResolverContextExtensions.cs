using HotChocolate.Resolvers;

namespace HotChocolate.Data
{
    public static class EntityFrameworkResolverContextExtensions
    {
        /// <summary>
        /// Retrieves a service of type <typeparamref name="TService"/>
        /// from the LocalContextData. 
        /// </summary>
        /// <param name="context">The resolver context.</param>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <returns>An instance of <typeparamref name="TService"/>.</returns>
        public static TService ScopedService<TService>(this IResolverContext context)
        {
            var scopedServiceName = typeof(TService).FullName ?? typeof(TService).Name;

            if (!context.LocalContextData.TryGetValue(scopedServiceName, out var value) ||
                value is not TService casted)
            {
                // todo: better message + resource string & maybe throwhelper
                throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage("Could not retrieve service from local context data.")
                        .SetPath(context.Path)
                        .AddLocation(context.Selection.SyntaxNode)
                        .Build());
            }

            return casted;
        }
    }
}