using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net;
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
        => field
            .Type<ListType<ObjectType<Foo>>>()
            .Resolve(async context =>
            {
                SearchRequest<Foo> searchRequest = client.CreateSearchRequest<Foo>(context)!;

                ISearchResponse<Foo> result =
                    await client.SearchAsync<Foo>(searchRequest);

                return result.Documents;
            });

    public static ValueTask<IRequestExecutor> BuildTestExecutorAsync(
        this IRequestExecutorBuilder builder) =>
        builder
            .UseRequest(
                next => async context =>
                {
                    await next(context);

                    if (context.ContextData.TryGetValue(nameof(SearchRequest), out var queryString))
                    {
                        context.Result =
                            QueryResultBuilder
                                .FromResult(context.Result!.ExpectQueryResult())
                                .SetContextData(nameof(SearchRequest), queryString)
                                .Create();
                    }
                })
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();

    public static IObjectFieldDescriptor UseTestReport(
        this IObjectFieldDescriptor descriptor,
        IElasticClient client) =>
        descriptor.Use(next => async context =>
        {
            await next(context);
            MemoryStream stream = new();
            SerializableData<SearchRequest> data = new(client.CreateSearchRequest(context)!);
            data.Write(stream, new ConnectionSettings(new InMemoryConnection()));
            context.ContextData[nameof(SearchRequest)] = Encoding.UTF8.GetString(stream.ToArray());
        });
}
