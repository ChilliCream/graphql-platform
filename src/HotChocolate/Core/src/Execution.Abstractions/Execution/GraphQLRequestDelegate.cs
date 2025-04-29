namespace HotChocolate.Execution;

/// <summary>
/// A function that can process a GraphQL request.
/// </summary>
/// <param name="context">
/// The <see cref="IGraphQLRequestContext"/> for the request.
/// </param>
/// <returns>
/// A task that represents the completion of request processing.
/// </returns>
public delegate ValueTask GraphQLRequestDelegate(
    IGraphQLRequestContext context);
