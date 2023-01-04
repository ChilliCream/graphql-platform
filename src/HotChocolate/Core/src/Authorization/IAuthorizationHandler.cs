using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Authorization;

/// <summary>
/// Represents the authorization process.
/// Implement this to handle the authorization of resolver data.
/// </summary>
public interface IAuthorizationHandler
{
    /// <summary>
    /// Executes the authorization for the specified authorization <paramref name="directive"/>.
    /// </summary>
    /// <param name="context">The current middleware context.</param>
    /// <param name="directive">The authorization directive.</param>
    /// <returns>
    /// Returns a value indicating if the current session is authorized to
    /// access the resolver data.
    /// </returns>
    ValueTask<AuthorizeResult> AuthorizeAsync(
        IMiddlewareContext context,
        AuthorizeDirective directive);

    ValueTask<AuthorizeResult> AuthorizeAsync(
        IReadOnlyList<AuthorizeDirective> directive);
}
