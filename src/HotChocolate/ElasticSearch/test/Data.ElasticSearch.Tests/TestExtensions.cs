using HotChocolate.Data.ElasticSearch.Execution;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using Snapshooter;
using Snapshooter.Xunit;

namespace HotChocolate.Data.ElasticSearch;

public static class TestExtensions
{
    public static void MatchQuerySnapshot(this IExecutionResult field)
    {
        field.ToJson().MatchSnapshot();
        field.ContextData?[nameof(SearchRequest)]
            ?.ToString()
            .MatchSnapshot(new SnapshotNameExtension("query"));
    }

    public static IObjectFieldDescriptor ResolveTestData<T>(
        this IObjectFieldDescriptor field,
        IElasticClient client,
        IEnumerable<T> data)
        where T : class
        => field
            .Resolve(_ => client.AsExecutable<T>());

    public static ValueTask<IRequestExecutor> BuildTestExecutorAsync(
        this IRequestExecutorBuilder builder) =>
        builder
            .UseRequest(
                next => async context =>
                {
                    await next(context);

                    if (context.ContextData.TryGetValue(nameof(IElasticSearchExecutable),
                            out var val) && val is IElasticSearchExecutable executable)
                    {
                        context.Result =
                            QueryResultBuilder
                                .FromResult(context.Result!.ExpectQueryResult())
                                .SetContextData(nameof(SearchRequest), executable.Print())
                                .Create();
                    }
                })
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();

    public static IObjectFieldDescriptor UseTestReport(this IObjectFieldDescriptor descriptor) =>
        descriptor.Use(next => async context =>
        {
            await next(context);
            if (context.Result is IElasticSearchExecutable executable)
            {
                context.ContextData[nameof(IElasticSearchExecutable)] = executable;
            }
        });
}
