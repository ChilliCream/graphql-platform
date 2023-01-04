using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Authorization;

/// <summary>
/// Represents the authorization process.
/// Implement this to handle the authorization of resolver data.
/// </summary>
public interface IAuthorizationHandler
{
    /// <summary>
    /// Executes the authorization validation for the specified <paramref name="directive"/>.
    /// </summary>
    /// <param name="context">
    /// The current middleware context.
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
    /// Executes the authorization validation for the specified <paramref name="directives"/>.
    /// </summary>
    /// <param name="context">
    ///
    /// </param>
    /// <param name="directives">
    /// The authorization directives.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns a value indicating if the current session is authorized to
    /// execute the GraphQL document.
    /// </returns>
    ValueTask<AuthorizeResult> AuthorizeAsync(
        AuthorizationContext context,
        IReadOnlyList<AuthorizeDirective> directives,
        CancellationToken cancellationToken = default);
}

public sealed class AuthorizationContext
{
    public AuthorizationContext(ISchema schema, IServiceProvider services, IDictionary<string, object?> contextData, DocumentNode document, string documentId)
    {
        Schema = schema;
        Services = services;
        ContextData = contextData;
        Document = document;
        DocumentId = documentId;
    }

    public ISchema Schema { get; }

    public IServiceProvider Services { get; }

    public IDictionary<string, object?> ContextData { get; }

    public DocumentNode Document { get; }

    public string DocumentId { get; }
}
