using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Nest;
using Snapshooter.Xunit;
using Squadron;
using Xunit;

namespace HotChocolate.Data.ElasticSearch;

public class IntegrationTests
    : IClassFixture<ElasticsearchResource<CustomElasticsearchDefaultOptions>>
{
    private ElasticsearchResource<CustomElasticsearchDefaultOptions> _resource;

    private readonly IReadOnlyList<Foo> _data = new[]
    {
        new Foo {Bar = "A"}, new Foo {Bar = "B"}, new Foo {Bar = "C"}
    };

    private readonly ElasticClient _client;

    public IntegrationTests(ElasticsearchResource<CustomElasticsearchDefaultOptions> resource)
    {
        _resource = resource;

        Uri uri = new Uri($"http://{resource.Instance.Address}:{resource.Instance.HostPort}");
        var connectionSettings = new ConnectionSettings(uri);
        connectionSettings.EnableDebugMode();
        connectionSettings.DisableDirectStreaming();
        connectionSettings.DefaultIndex($"{Guid.NewGuid():N}");

        _client = new ElasticClient(connectionSettings);
    }

    public class TestOperationType : StringOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
        }
    }

    private async Task SetupElastic()
    {
        foreach (Foo element in _data)
        {
            await _client.IndexAsync(new IndexRequest<Foo>(element));
        }
    }

    [Fact]
    public async Task Test()
    {
        await SetupElastic();

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<Foo>(x => x.Field(x => x.Bar).Type<TestOperationType>())
                .UseTestReport()
                .Type<ListType<ObjectType<Foo>>>()
                .Resolve(async context =>
                {
                    SearchRequest<Foo> searchRequest = context.CreateSearchRequest<Foo>()!;
                    ISearchResponse<Foo> result =
                        await _client.SearchAsync<Foo>(searchRequest);
                    return new Foo[0];
                }))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: {bar: { eq: ""A"" }}) {
                bar
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);

        result.ToJson().MatchSnapshot();
    }
}

public static class TestExtensions
{
    public static ValueTask<IRequestExecutor> BuildTestExecutorAsync(
        this IRequestExecutorBuilder builder) =>
        builder
            .UseRequest(
                next => async context =>
                {
                    await next(context);
                    if (context.ContextData.TryGetValue("sql", out var queryString))
                    {
                        context.Result =
                            QueryResultBuilder
                                .FromResult(context.Result!.ExpectQueryResult())
                                .SetContextData("sql", queryString)
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
            MemoryStream stream = new();
            SerializableData<SearchRequest> data = new(context.CreateSearchRequest()!);
            data.Write(stream, new ConnectionSettings(new InMemoryConnection()));
            context.ContextData[nameof(SearchRequest)] = Encoding.UTF8.GetString(stream.ToArray());
        });
}

public class Foo
{
    public string Bar { get; set; } = string.Empty;
}

/// <summary>
/// Default Elasticsearch resource options
/// </summary>
public class CustomElasticsearchDefaultOptions : ElasticsearchDefaultOptions
{
    /// <summary>
    /// Configure resource options
    /// </summary>
    /// <param name="builder"></param>
    public override void Configure(ContainerResourceBuilder builder)
    {
        base.Configure(builder);
        var name = "elastic";
        builder
            .Name(name)
            .Image("docker.elastic.co/elasticsearch/elasticsearch:7.16.3")
            .InternalPort(9200)
            .WaitTimeout(120)
            .AddEnvironmentVariable("discovery.type=single-node")
            .AddEnvironmentVariable($"cluster.name={name}");
    }
}
