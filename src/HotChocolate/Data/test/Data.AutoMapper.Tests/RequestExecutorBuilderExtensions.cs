using System;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Projections;

public static class RequestExecutorBuilderExtensions
{
    public static IObjectFieldDescriptor UseSqlLogging(this IObjectFieldDescriptor descriptor)
    {
        return descriptor
            .Use(
                next => async context =>
                {
                    await next(context);

                    if (context.Result is IQueryable queryable)
                    {
                        try
                        {
                            context.ContextData["sql"] = queryable.ToQueryString();
                            context.ContextData["expression"] = queryable.Expression.Print();
                        }
                        catch (Exception ex)
                        {
                            context.ContextData["sql"] = ex.Message;
                        }
                    }
                });
    }

    public static IRequestExecutorBuilder UseSqlLogging(this IRequestExecutorBuilder builder)
    {
        return builder
            .UseRequest(
                next => async context =>
                {
                    await next(context);
                    if (context.Result is IReadOnlyQueryResult result &&
                        context.ContextData.TryGetValue("sql", out object? queryString)&&
                        context.ContextData.TryGetValue("expression", out object? expression))
                    {
                        context.Result =
                            QueryResultBuilder
                                .FromResult(result)
                                .SetContextData("sql", queryString)
                                .SetContextData("expression", expression)
                                .Create();
                    }
                })
            .UseDefaultPipeline();
    }
}
