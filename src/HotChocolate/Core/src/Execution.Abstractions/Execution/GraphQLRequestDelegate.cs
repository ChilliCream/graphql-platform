namespace HotChocolate.Execution;

/// <summary>
/// A function that can process a GraphQL request.
/// </summary>
/// <param name="context">
/// The <see cref="GraphQLRequestContext"/> for the request.
/// </param>
/// <returns>
/// A task that represents the completion of request processing.
/// </returns>
public delegate ValueTask GraphQLRequestDelegate(
    GraphQLRequestContext context);
