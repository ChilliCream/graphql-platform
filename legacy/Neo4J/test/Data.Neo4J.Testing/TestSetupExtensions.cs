using HotChocolate.Data.Neo4J.Execution;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J.Testing;

public static class TestSetupExtensions
{
    public static IObjectFieldDescriptor SetupNeo4JTestField<TEntity>(
        this IObjectFieldDescriptor fieldDescriptor,
        Func<string?, IAsyncSession> session)
    {
        return fieldDescriptor.Resolve(ctx =>
        {
            var executable = new Neo4JExecutable<TEntity>(session(default));
            ctx.ContextData[nameof(INeo4JExecutable)] = executable;
            return executable;
        });
    }

    public static IRequestExecutorBuilder SetupNeo4JTestResponse(this IRequestExecutorBuilder builder)
    {
        return builder.UseRequest(
            next => async context =>
            {
                await next(context);

                if (context.ContextData.TryGetValue(nameof(INeo4JExecutable), out var query)
                    && query is IExecutable executable)
                {
                    context.Result =
                        QueryResultBuilder
                            .FromResult(context.Result!.ExpectQueryResult())
                            .SetContextData("query", executable.Print())
                            .Create();
                }
            });
    }
}
