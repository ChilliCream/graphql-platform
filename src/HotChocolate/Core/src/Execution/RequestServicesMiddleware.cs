using System;

namespace HotChocolate.Execution
{
    /// <summary>
    /// Defines request middleware that can be added to the GraphQL request pipeline.
    /// </summary>
    public delegate RequestDelegate RequestServicesMiddleware(
        IServiceProvider services, RequestDelegate next);
}
