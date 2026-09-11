using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0, warmupCount: 5, iterationCount: 10)]
public class DeferredPolicyMaterializationBenchmark
{
    private const string QueryText =
        """
        {
          users {
            profile { level2 { level3 { level4 { level5 { level6 { level7 { level8 { immediate } } } } } } } }
            ... @defer {
              profile { level2 { level3 { level4 { level5 { level6 { level7 { level8 { secret } } } } } } } }
            }
          }
        }
        """;

    private IRequestExecutor _executor = null!;
    private StaticClient _client = null!;

    [Params(32, 256)]
    public int UserCount;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        _client = new StaticClient(CreatePayload(UserCount));
        var builder = services
            .AddGraphQLGateway()
            .ModifyOptions(options => options.EnableDefer = true)
            .AddInMemoryConfiguration(ComposeSchema());
        builder.ConfigureSchemaServices(
            (_, schemaServices) => schemaServices.AddSingleton<IPolicyProvider>(
                new StaticPolicyProvider(new DenyPolicy())));
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new StaticClientFactory(_client));
        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new StaticClientConfiguration("a")));
        _executor = await services.BuildGatewayAsync(CancellationToken.None);

        if (await ExecuteAsync() != 2 || _client.CallCount != 1)
        {
            throw ThrowHelper.InvalidMaterializationProbe();
        }
    }

    [Benchmark]
    public Task<int> MaterializeDeniedDeepListAsync() => ExecuteAsync();

    internal static async Task<int> RunProbeAsync(int userCount)
    {
        var benchmark = new DeferredPolicyMaterializationBenchmark { UserCount = userCount };
        await benchmark.SetupAsync();
        var initialCallCount = benchmark._client.CallCount;
        var responseCount = await benchmark.MaterializeDeniedDeepListAsync();

        if (responseCount != 2 || benchmark._client.CallCount != initialCallCount + 1)
        {
            throw ThrowHelper.InvalidMaterializationProbe();
        }

        return responseCount;
    }

    private async Task<int> ExecuteAsync()
    {
        await using var result = await _executor.ExecuteAsync(QueryText, CancellationToken.None);
        await using var stream = result.ExpectResponseStream();
        var responseCount = 0;

        await foreach (var _ in stream.ReadResultsAsync())
        {
            responseCount++;
        }

        return responseCount;
    }

    private static DocumentNode ComposeSchema()
    {
        var composer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "a",
                    """
                    enum PolicyDenialBehavior { NULL ERROR ABORT }

                    directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                      repeatable on OBJECT | FIELD_DEFINITION

                    type Query { users: [User] }
                    type User { profile: Level1 }
                    type Level1 { level2: Level2 }
                    type Level2 { level3: Level3 }
                    type Level3 { level4: Level4 }
                    type Level4 { level5: Level5 }
                    type Level5 { level6: Level6 }
                    type Level6 { level7: Level7 }
                    type Level7 { level8: Level8 }
                    type Level8 {
                      immediate: String
                      secret: String @policy(names: "CanReadSecret", onDenied: ERROR)
                    }
                    """)
            ],
            new SchemaComposerOptions(),
            new CompositionLog());

        return composer.Compose().Value.ToSyntaxNode();
    }

    private static byte[] CreatePayload(int count)
    {
        var payload = new StringBuilder("{\"data\":{\"users\":[");

        for (var i = 0; i < count; i++)
        {
            if (i > 0)
            {
                payload.Append(',');
            }

            payload.Append("{\"profile\":{\"level2\":{\"level3\":{\"level4\":{");
            payload.Append("\"level5\":{\"level6\":{\"level7\":{\"level8\":{\"immediate\":\"");
            payload.Append(i);
            payload.Append("\"}}}}}}}}}");
        }

        payload.Append("]}}");
        return Encoding.UTF8.GetBytes(payload.ToString());
    }

    private sealed class DenyPolicy : IPolicy
    {
        public string Name => "CanReadSecret";

        public PolicyRequirements Requirements => PolicyRequirements.Empty;

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
        {
            context.Deny(0, "denied by benchmark policy");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class StaticPolicyProvider(IPolicy policy) : IPolicyProvider
    {
        public IDisposable Subscribe(IObserver<ImmutableArray<IPolicy>> observer)
        {
            observer.OnNext([policy]);
            return Subscription.Instance;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private sealed class Subscription : IDisposable
        {
            public static readonly Subscription Instance = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed class StaticClient(byte[] payload) : ISourceSchemaClient
    {
        private int _callCount;

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public int CallCount => Volatile.Read(ref _callCount);

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);
            var arena = context.MemorySource.GetNextArena();
            var document = SourceResultDocument.Parse(arena, payload, payload.Length);
            await Task.Yield();
            yield return new SourceSchemaResult(CompactPath.Root, document);
        }

        public async IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
            OperationPlanContext context,
            ImmutableArray<SourceSchemaClientRequest> requests,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }

        public async IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class StaticClientFactory(StaticClient client) : ISourceSchemaClientFactory
    {
        public bool CanHandle(ISourceSchemaClientConfiguration configuration)
            => configuration is StaticClientConfiguration;

        public ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            ISourceSchemaClientConfiguration configuration)
            => client;
    }

    private sealed class StaticClientConfiguration(string name)
        : ISourceSchemaClientConfiguration
    {
        public string Name { get; } = name;

        public SupportedOperationType SupportedOperations => SupportedOperationType.Query;
    }

    private static class ThrowHelper
    {
        public static InvalidOperationException InvalidMaterializationProbe()
            => new(
                "The deferred materialization benchmark must produce two responses and one source call.");
    }
}
