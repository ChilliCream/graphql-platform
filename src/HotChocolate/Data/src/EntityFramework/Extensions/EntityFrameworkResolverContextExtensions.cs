using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using static HotChocolate.Data.Properties.EntityFrameworkResources;

namespace HotChocolate.Data;

public static class EntityFrameworkResolverContextExtensions
{
    /// <summary>
    /// Retrieves an instance of <typeparamref name="TDbContext"/>
    /// from the LocalContextData.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <typeparam name="TDbContext">
    /// The type of the <see cref="Microsoft.EntityFrameworkCore.DbContext"/>.
    /// </typeparam>
    /// <returns>An instance of <typeparamref name="TDbContext"/>.</returns>
    public static TDbContext DbContext<TDbContext>(this IResolverContext context)
        where TDbContext : DbContext
    {
        var dbContextName = typeof(TDbContext).FullName ?? typeof(TDbContext).Name;

        if (!context.LocalContextData.TryGetValue(dbContextName, out var value) ||
            value is not TDbContext casted)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(ResolverContext_DbContext_MissingFromLocalState, dbContextName)
                    .SetPath(context.Path)
                    .AddLocation(context.Selection.SyntaxNode)
                    .Build());
        }

        return casted;
    }
}
