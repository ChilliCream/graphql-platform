namespace HotChocolate.Execution
{
    /// <summary>
    /// Defines request middleware that can be added to the GraphQL request pipeline.
    /// </summary>
    public delegate RequestDelegate RequestCoreMiddleware(
        IRequestCoreMiddlewareContext context,
        RequestDelegate next);
}
