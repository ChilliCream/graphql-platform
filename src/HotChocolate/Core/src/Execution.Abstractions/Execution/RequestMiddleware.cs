namespace HotChocolate.Execution;

/// <summary>
/// A function that creates a GraphQL request delegate.
/// </summary>
/// <param name="context">
/// The factory context.
/// </param>
/// <param name="next">
/// The next middleware in the pipeline.
/// </param>
/// <returns>
/// Returns a <see cref="RequestDelegate"/> that can process a GraphQL request.
/// </returns>
public delegate RequestDelegate RequestMiddleware(
    RequestMiddlewareFactoryContext context,
    RequestDelegate next);
