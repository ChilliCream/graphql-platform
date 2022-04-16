using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        where T : class, IHasId
        => field
            .Type<ListType<ObjectType<T>>>()
            .Resolve(async context =>
            {
                SearchDescriptor<T> searchRequest = client.CreateSearchDescriptor<T>(context)!;
                searchRequest.Explain();

                ISearchResponse<T> result =
                    await client.SearchAsync<T>(searchRequest);

                var ids = result.Hits.Select(x => x.Source.Id).ToHashSet();
                return data.Where(x => ids.Contains(x.Id)).ToArray();
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
