using HotChocolate.Resolvers;

namespace HotChocolate.Authorization;

/// <summary>
/// The authorization handler abstracts the authorization logic that is applied to schema objects.
/// </summary>
public interface IAuthorizationHandler
{
    /// <summary>
    /// Executes the authorization logic during the GraphQL request execution
    /// for the specified <paramref name="directive"/>.
    /// </summary>
    /// <param name="context">
    /// The middleware context.
    /// </param>
    /// <param name="directive">
    /// The authorization directive.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns a value indicating if the current session is authorized to
    /// access the resolver data.
    /// </returns>
    ValueTask<AuthorizeResult> AuthorizeAsync(
        IMiddlewareContext context,
        AuthorizeDirective directive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the authorization logic during the GraphQL request validation.
    /// The validation will collect all authorization policies for a provided GraphQL
    /// request document and validate them in one batch call before the request execution
    /// has begun.
    /// </summary>
    /// <param name="context">
    /// The authorization context.
    /// </param>
    /// <param name="directives">
    /// The authorization directives.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns a value indicating if the current session is authorized to
    /// execute the GraphQL request document.
    /// </returns>
    ValueTask<AuthorizeResult> AuthorizeAsync(
        AuthorizationContext context,
        IReadOnlyList<AuthorizeDirective> directives,
        CancellationToken cancellationToken = default);
}
