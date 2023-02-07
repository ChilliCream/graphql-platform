using HotChocolate.Resolvers;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace HotChocolate.Data.Raven;

/// <summary>
/// Common extension for <see cref="IResolverContext"/> for RavenDb
/// </summary>
public static class RavenResolverContextExtensions
{
    /// <summary>
    /// Retrieves an instance of <see cref="IAsyncDocumentSession"/> and registers it for disposal
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <returns>An instance of <see cref="IAsyncDocumentSession"/>.</returns>
    public static IAsyncDocumentSession AsyncSession(this IResolverContext context)
    {
        var session = context.Service<IDocumentStore>().OpenAsyncSession();

        var middlewareContext = (IMiddlewareContext)context;
        middlewareContext.RegisterForCleanup(() =>
        {
            session.Dispose();
            return ValueTask.CompletedTask;
        });

        return session;
    }
}
