using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Language;
using HotChocolate.Transport.Formatters;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

#nullable enable

namespace Fusion.Execution.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(ParityConfig))]
public class ApolloParityBenchmark
{
    private static readonly int[] s_querySizes = [1, 10, 50];

    private IRequestExecutor _apollo = default!;
    private IRequestExecutor _native = default!;
    private readonly Dictionary<int, string> _queries = new();

    [Params(1, 10, 50)]
    public int N;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        var cancellationToken = CancellationToken.None;

        _native = await BuildNativeGatewayAsync(cancellationToken);
        _apollo = await BuildApolloGatewayAsync(cancellationToken);

        foreach (var size in s_querySizes)
        {
            _queries[size] = "{ products(first: " + size + ") { id name price } }";
        }

        await AssertCorrectnessAsync(_native, "native", cancellationToken);
        await AssertCorrectnessAsync(_apollo, "apollo", cancellationToken);

        foreach (var query in _queries.Values)
        {
            await ExecuteAndCountProductsAsync(_native, query, cancellationToken);
            await ExecuteAndCountProductsAsync(_apollo, query, cancellationToken);
        }
    }

    [GlobalCleanup]
    public Task CleanupAsync()
        => Task.CompletedTask;

    [Benchmark(Baseline = true)]
    public Task<int> Native()
        => ExecuteAndCountProductsAsync(_native, _queries[N], CancellationToken.None);

    [Benchmark]
    public Task<int> Apollo()
        => ExecuteAndCountProductsAsync(_apollo, _queries[N], CancellationToken.None);

    private static async Task<IRequestExecutor> BuildNativeGatewayAsync(
        CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();

        services
            .AddGraphQL("a")
            .AddQueryType<NativeA.Query>()
            .AddSourceSchemaDefaults();

        services
            .AddGraphQL("b")
            .AddQueryType<NativeB.Query>()
            .AddSourceSchemaDefaults();

        services
            .AddGraphQLGateway()
            .AddInMemorySchema("a")
            .AddInMemorySchema("b");

        return await services.BuildGatewayAsync(cancellationToken);
    }

    private static async Task<IRequestExecutor> BuildApolloGatewayAsync(
        CancellationToken cancellationToken)
    {
        var apolloDocument = await ComposeApolloSchemaAsync(cancellationToken);
        var services = new ServiceCollection();

        RegisterApolloSubgraphs(services);

        var builder = services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(apolloDocument);

        services.RemoveAll<ISourceSchemaClientFactory>();
        services.AddSingleton(
            sp => new InMemorySourceSchemaClientFactory(
                sp.GetRequiredService<IRequestExecutorProvider>(),
                sp.GetRequiredService<IRequestExecutorEvents>(),
                JsonResultFormatter.Default));
        services.AddSingleton<ISourceSchemaClientFactory>(
            sp => sp.GetRequiredService<InMemorySourceSchemaClientFactory>());

        foreach (var schemaName in new[] { "a", "b" })
        {
            FusionSetupUtilities.Configure(
                builder,
                setup => setup.ClientConfigurationModifiers.Add(
                    _ => new InMemorySourceSchemaClientConfiguration(schemaName)));
        }

        return await services.BuildGatewayAsync(cancellationToken);
    }

    private static async Task<DocumentNode> ComposeApolloSchemaAsync(CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();

        RegisterApolloSubgraphs(services);

        await using var serviceProvider = services.BuildServiceProvider();
        var executorProvider = serviceProvider.GetRequiredService<IRequestExecutorProvider>();
        var sdlA = await GetApolloSdlAsync(executorProvider, "a", cancellationToken);
        var sdlB = await GetApolloSdlAsync(executorProvider, "b", cancellationToken);
        var options = new SchemaComposerOptions();

        options.SourceSchemas["a"] = new SourceSchemaOptions
        {
            Preprocessor = new SourceSchemaPreprocessorOptions
            {
                InferKeysFromLookups = false
            }
        };

        options.SourceSchemas["b"] = new SourceSchemaOptions
        {
            Preprocessor = new SourceSchemaPreprocessorOptions
            {
                InferKeysFromLookups = false
            }
        };

        var log = new CompositionLog();
        var result = new SchemaComposer(
            [
                new SourceSchemaText("a", sdlA),
                new SourceSchemaText("b", sdlB)
            ],
            options,
            log).Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                "Apollo federation composition failed: " + string.Join(Environment.NewLine, log));
        }

        return result.Value.ToSyntaxNode();
    }

    private static void RegisterApolloSubgraphs(IServiceCollection services)
    {
        services
            .AddGraphQL("a")
            .AddApolloFederation()
            .AddQueryType<ApolloA.QueryType>()
            .AddType<ApolloA.ProductType>();

        services
            .AddGraphQL("b")
            .AddApolloFederation()
            .AddQueryType<ApolloB.QueryType>()
            .AddType<ApolloB.ProductType>();
    }

    private static async Task<string> GetApolloSdlAsync(
        IRequestExecutorProvider executorProvider,
        string schemaName,
        CancellationToken cancellationToken)
    {
        var executor = await executorProvider.GetExecutorAsync(schemaName, cancellationToken);
        await using var result =
            await executor.ExecuteAsync("{ _service { sdl } }", cancellationToken);
        var json = result.ToJson();

        using var document = JsonDocument.Parse(json);

        return document
            .RootElement
            .GetProperty("data")
            .GetProperty("_service")
            .GetProperty("sdl")
            .GetString()!;
    }

    private static async Task AssertCorrectnessAsync(
        IRequestExecutor executor,
        string arm,
        CancellationToken cancellationToken)
    {
        const string query = "{ products(first: 2) { id name price } }";
        await using var result = await executor.ExecuteAsync(query, cancellationToken);
        var json = result.ToJson();

        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("data", out var data)
            || !data.TryGetProperty("products", out var products)
            || products.ValueKind != JsonValueKind.Array
            || products.GetArrayLength() < 2)
        {
            throw new InvalidOperationException(
                "Correctness gate failed for " + arm + ": " + json);
        }

        foreach (var product in products.EnumerateArray())
        {
            if (!product.TryGetProperty("price", out var price)
                || price.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                throw new InvalidOperationException(
                    "Correctness gate failed for " + arm + ": " + json);
            }
        }
    }

    private static async Task<int> ExecuteAndCountProductsAsync(
        IRequestExecutor executor,
        string query,
        CancellationToken cancellationToken)
    {
        await using var result = await executor.ExecuteAsync(query, cancellationToken);
        var json = result.ToJson();

        using var document = JsonDocument.Parse(json);

        return document
            .RootElement
            .GetProperty("data")
            .GetProperty("products")
            .GetArrayLength();
    }

    public sealed class ParityConfig : ManualConfig
    {
        public ParityConfig()
        {
            AddJob(Job.ShortRun.WithToolchain(InProcessEmitToolchain.Instance));
            AddDiagnoser(MemoryDiagnoser.Default);
            AddColumn(StatisticColumn.P95);
        }
    }

    private static class NativeA
    {
        public sealed class Query
        {
            public IEnumerable<Product> GetProducts(int first)
                => Enumerable
                    .Range(1, first)
                    .Select(i => new Product(i, "Product " + i));

            [Lookup]
            [Internal]
            public Product? GetProductById(int id)
                => id >= 1 ? new Product(id, "Product " + id) : null;
        }

        [EntityKey("id")]
        public sealed record Product(int Id, string Name);
    }

    private static class NativeB
    {
        public sealed class Query
        {
            [Lookup]
            [Internal]
            public Product? GetProductById(int id)
                => id >= 1 ? new Product(id) : null;
        }

        [EntityKey("id")]
        [GraphQLName("Product")]
        public sealed record Product(int Id)
        {
            public double GetPrice()
                => Id * 1.5;
        }
    }

    private static class ApolloA
    {
        public sealed class QueryType : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name(OperationTypeNames.Query);
                descriptor
                    .Field("products")
                    .Type<NonNullType<ListType<NonNullType<ObjectType<Product>>>>>()
                    .Argument("first", a => a.Type<NonNullType<IntType>>())
                    .Resolve(ctx => Enumerable
                        .Range(1, ctx.ArgumentValue<int>("first"))
                        .Select(i => new Product
                        {
                            Id = i.ToString(),
                            Name = "Product " + i
                        })
                        .ToArray());
            }
        }

        public sealed class ProductType : ObjectType<Product>
        {
            protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
            {
                descriptor.Key("id").ResolveReferenceWith(_ => Resolve(default!));
            }

            private static Product Resolve(string id)
                => new()
                {
                    Id = id,
                    Name = "Product " + id
                };
        }

        public sealed class Product
        {
            public string Id { get; set; } = default!;

            public string Name { get; set; } = default!;
        }
    }

    private static class ApolloB
    {
        public sealed class QueryType : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name(OperationTypeNames.Query);
                descriptor
                    .Field("noop")
                    .Type<StringType>()
                    .Resolve("ok");
            }
        }

        public sealed class ProductType : ObjectType<Product>
        {
            protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
            {
                descriptor.Key("id").ResolveReferenceWith(_ => Resolve(default!));
            }

            private static Product Resolve(string id)
            {
                var parsedId = int.Parse(id);

                return new Product
                {
                    Id = id,
                    Price = parsedId * 1.5
                };
            }
        }

        public sealed class Product
        {
            public string Id { get; set; } = default!;

            public double Price { get; set; }
        }
    }
}
