using HotChocolate.Resolvers;

namespace HotChocolate.Data
{
    public static class EntityFrameworkResolverContextExtensions
    {
        public static TDbContext DbContext<TDbContext>(this IResolverContext context)
        {
            var scopedServiceName = typeof(TDbContext).FullName ?? typeof(TDbContext).Name;

            if (!context.LocalContextData.TryGetValue(scopedServiceName, out var value) ||
                value is not TDbContext casted)
            {
                // todo: better message + resource string & maybe throwhelper
                throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage("Could not retrieve DbContext")
                        .SetPath(context.Path)
                        .AddLocation(context.Selection.SyntaxNode)
                        .Build());
            }

            return casted;
        }
    }
}