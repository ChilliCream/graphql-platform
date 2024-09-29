using HotChocolate.Execution;
using HotChocolate.Execution.Options;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace HotChocolate;

public static class ExecutionSchemaExtensions
{
    public static IRequestExecutor MakeExecutable(
        this ISchema schema)
        => MakeExecutable(schema, _ => { });

    public static IRequestExecutor MakeExecutable(
        this ISchema schema,
        Action<RequestExecutorOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(configure);

        return new ServiceCollection()
            .AddGraphQL()
            .Configure(o => o.Schema = schema)
            .ModifyRequestOptions(configure)
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync()
            .Result;
    }
}
