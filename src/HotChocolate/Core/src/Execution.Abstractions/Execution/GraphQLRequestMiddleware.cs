namespace HotChocolate.Execution;

/// <summary>
/// A function that creates a GraphQL request delegate.
/// </summary>
/// <param name="context">
/// The factory context.
/// </param>
/// <returns>
/// Returns a <see cref="GraphQLRequestDelegate"/> that can process a GraphQL request.
/// </returns>
public delegate GraphQLRequestDelegate GraphQLRequestMiddleware(
    GraphQLMiddlewareFactoryContext context,
    GraphQLRequestDelegate next);
