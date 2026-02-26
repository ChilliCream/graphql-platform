using HotChocolate.Data.ElasticSearch.Execution;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Nest;

namespace HotChocolate.Data.ElasticSearch;

public static class TestExtensions
{
    public static void MatchQuerySnapshot(this IExecutionResult field)
    {
        field.ToJson().MatchSnapshot();
        field.ContextData?[nameof(SearchRequest)]
            ?.ToString()
            .MatchSnapshot(postFix: "query");
    }

    public static IObjectFieldDescriptor ResolveTestData<T>(
        this IObjectFieldDescriptor field,
        IElasticClient client)
        where T : class
        => field
            .Resolve(_ => client.AsExecutable<T>());

    public static ValueTask<IRequestExecutor> BuildTestExecutorAsync(
        this IRequestExecutorBuilder builder) =>
        builder
            .UseRequest(
                (_, next) => async context =>
                {
                    await next(context);

                    if (context.ContextData.TryGetValue(nameof(IElasticSearchExecutable),
                            out var val) && val is IElasticSearchExecutable executable)
                    {
                        var result = context.Result.ExpectOperationResult();
                        result.ContextData = result.ContextData.SetItem(
                            nameof(SearchRequest),
                            executable.Print());
                    }
                })
            .UseDefaultPipeline()
            .BuildRequestExecutorAsync();

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
