using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Nest;
using Snapshooter;
using Snapshooter.Xunit;
using Squadron;
using Xunit;

namespace HotChocolate.Data.ElasticSearch;

public class TestBase
{
    public TestBase(ElasticsearchResource resource)
    {
        Resource = resource;

        Uri uri = new($"http://{resource.Instance.Address}:{resource.Instance.HostPort}");
        var connectionSettings = new ConnectionSettings(uri);
        connectionSettings.EnableDebugMode();
        connectionSettings.DisableDirectStreaming();
        connectionSettings.DefaultIndex(DefaultIndexName);

        Client = new ElasticClient(connectionSettings);
    }

    protected ElasticsearchResource Resource { get; }

    protected ElasticClient Client { get; }

    protected string DefaultIndexName { get; }= $"{Guid.NewGuid():N}";

    protected async Task SetupMapping<T>()
    where T : class
    {
        await Client.Indices
            .CreateAsync(DefaultIndexName, c => c.Map(x => x.AutoMap<T>()));

    }
    protected async Task IndexDocuments<T>(IEnumerable<T> data)
        where T : class, IHasId
    {
        await SetupMapping<T>();
        foreach (T element in data)
        {
            await Client.IndexDocumentAsync(new IndexRequest<T>(element));
        }
    }
}

public interface IHasId
{
    public string Id { get; }
}

public class ElasticSearchStringTests : TestBase, IClassFixture<ElasticsearchResource>
{
    private readonly IReadOnlyList<Foo> _data = new[]
    {
        new Foo {Id = "A", Bar = "A"},
        new Foo {Id = "B", Bar = "B"},
        new Foo {Id = "C", Bar = "C"}
    };

    public ElasticSearchStringTests(ElasticsearchResource resource) : base(resource)
    {
    }

    public class TestOperationType : StringOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
        }
    }

    public class FooFilterType : FilterInputType<Foo>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(x => x.Bar).Type<TestOperationType>();
            descriptor.Field(x => x.Id).Type<TestOperationType>();
        }
    }

    [Fact]
    public async Task ElasticSearch_SingleField()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport(Client)
                .ResolveTestData(Client, _data))
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
        result.MatchQuerySnapshot();
    }

    public class Foo : IHasId
    {
        public string Bar { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;
    }
}

public class IntegrationTests : TestBase, IClassFixture<ElasticsearchResource>
{
    private readonly IReadOnlyList<Foo> _data = new[]
    {
        new Foo {Id = "A", Bar = "A", Qux = "A", Baz = new Baz() {Bar = "A", Qux = "A",}},
        new Foo {Id = "B", Bar = "B", Qux = "B", Baz = new Baz() {Bar = "B", Qux = "B",}},
        new Foo {Id = "C", Bar = "C", Qux = "C", Baz = new Baz() {Bar = "C", Qux = "C",}}
    };

    public IntegrationTests(ElasticsearchResource resource) : base(resource)
    {
    }

    public class TestOperationType : StringOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
        }
    }

    public class FooFilterType : FilterInputType<Foo>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(x => x.Bar).Type<TestOperationType>();
            descriptor.Field(x => x.Id).Type<TestOperationType>();
            descriptor.Field(x => x.Qux).Type<TestOperationType>();
            descriptor.Field(x => x.Baz).Type<BazFilterType>();
        }
    }

    public class BazFilterType : FilterInputType<Baz>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Baz> descriptor)
        {
            descriptor.Field(x => x.Bar).Type<TestOperationType>();
            descriptor.Field(x => x.Qux).Type<TestOperationType>();
        }
    }

    [Fact]
    public async Task ElasticSearch_SingleField()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport(Client)
                .ResolveTestData(Client, _data))
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
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_MultipleField()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport(Client)
                .ResolveTestData(Client, _data))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: {qux: { eq: ""A"" }, bar: { eq: ""A"" }}) {
                bar
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_AndField()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport(Client)
                .ResolveTestData(Client, _data))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: {bar: {and:[{ eq: ""B"" },{ eq: ""A"" }]}}) {
                bar
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_OrField()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport(Client)
                .ResolveTestData(Client, _data))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: {bar: {or:[{ eq: ""B"" },{ eq: ""A"" }]}}) {
                bar
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_DeepField()
    {
        await IndexDocuments(_data);

        IRequestExecutor executorAsync = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<FooFilterType>()
                .UseTestReport(Client)
                .ResolveTestData(Client, _data))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();

        const string query = @"
        {
            test(where: { baz :{ bar: { eq: ""A"" }}}) {
                bar
            }
        }
        ";

        IExecutionResult result = await executorAsync.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    public class Foo : IHasId
    {
        public string Bar { get; set; } = string.Empty;

        public string Qux { get; set; } = string.Empty;

        public Baz? Baz { get; set; }

        public string Id { get; set; } = string.Empty;
    }

    public class Baz
    {
        public string Bar { get; set; } = string.Empty;

        public string Qux { get; set; } = string.Empty;
    }
}
