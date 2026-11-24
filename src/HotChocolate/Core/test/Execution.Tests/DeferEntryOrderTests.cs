using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Processing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution;

public class DeferEntryOrderTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    private const string DeferredQuery =
        """
        {
          slowRoot
          products {
            name
            ... @defer(label: "P") {
              slowProduct
              reviews {
                body
                ... @defer(label: "C") {
                  detail
                }
              }
            }
          }
        }
        """;

    private const string OuterGroupQuery =
        """
        {
          ... @defer(label: "G") {
            slowRoot
            products {
              name
              ... @defer(label: "P") {
                slowProduct
                reviews {
                  body
                  ... @defer(label: "C") {
                    detail
                  }
                }
              }
            }
          }
        }
        """;

    private const string PlainQuery =
        """
        {
          slowRoot
          products {
            name
            slowProduct
            reviews {
              body
              detail
            }
          }
        }
        """;

    [Fact]
    public async Task Execute_Should_ListParentEntryFirst_When_NestedGroupSettlesBeforeItsParent()
    {
        // arrange
        var gates = new Gates();
        var executor = await CreateExecutorAsync(gates);
        gates.Root.TrySetResult();

        // act
        var payloads = await ReadPayloadsAsync(
            executor,
            DeferredQuery,
            async () =>
            {
                gates.Review.TrySetResult();
                await gates.WaitForSettledGroupsAsync(1);
                gates.Product.TrySetResult();
            });

        // assert
        await MatchAsync(executor, gates, payloads);
    }

    [Fact]
    public async Task Execute_Should_ListEnclosingEntriesFirst_When_ParentGroupSettlesBeforeNestedGroup()
    {
        // arrange
        var gates = new Gates();
        var executor = await CreateExecutorAsync(gates);

        // act
        // the outer group delivers the products, so all three groups settle into its payload.
        var payloads = await ReadPayloadsAsync(
            executor,
            OuterGroupQuery,
            async () =>
            {
                gates.Product.TrySetResult();
                await gates.WaitForSettledGroupsAsync(1);
                gates.Review.TrySetResult();
                await gates.WaitForSettledGroupsAsync(2);
                gates.Root.TrySetResult();
            });

        // assert
        await MatchAsync(executor, gates, payloads);
    }

    private static async Task MatchAsync(IRequestExecutor executor, Gates gates, List<string> payloads)
    {
        var expected = await ExecutePlainAsync(executor, gates);
        var merged = MergeInArrayOrder(payloads);

        Assert.Equal(expected, merged);

        var snapshot = new Snapshot();

        for (var i = 0; i < payloads.Count; i++)
        {
            snapshot.Add(payloads[i], $"Payload {i}", "json");
        }

        snapshot.Add(merged, "Merged in array order", "json");
        snapshot.MatchMarkdown();
    }

    private static async Task<List<string>> ReadPayloadsAsync(
        IRequestExecutor executor,
        string query,
        Func<Task> settleAfterInitialPayload)
    {
        return await Task.Run(
                async () =>
                {
                    var payloads = new List<string>();
                    await using var result = await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);
                    var stream = Assert.IsType<ResponseStream>(result);

                    await foreach (var payload in stream
                        .ReadResultsAsync()
                        .WithCancellation(TestContext.Current.CancellationToken))
                    {
                        await using var cleanup = payload;
                        payloads.Add(payload.ToJson());

                        if (payloads.Count == 1)
                        {
                            await settleAfterInitialPayload();
                        }
                    }

                    return payloads;
                },
                TestContext.Current.CancellationToken)
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
    }

    private static async Task<string> ExecutePlainAsync(IRequestExecutor executor, Gates gates)
    {
        gates.Root.TrySetResult();
        gates.Product.TrySetResult();
        gates.Review.TrySetResult();

        await using var result = await executor.ExecuteAsync(PlainQuery, TestContext.Current.CancellationToken);
        var data = JsonNode.Parse(result.ToJson())!["data"]!;
        return data.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    // applies the incremental entries of every payload in the order in which they are listed.
    private static string MergeInArrayOrder(List<string> payloads)
    {
        JsonNode? data = null;
        var paths = new Dictionary<string, JsonArray>();

        foreach (var json in payloads)
        {
            var payload = JsonNode.Parse(json)!;
            data ??= payload["data"]!.DeepClone();

            if (payload["pending"] is JsonArray pending)
            {
                foreach (var entry in pending)
                {
                    paths[entry!["id"]!.GetValue<string>()] = entry["path"]!.AsArray();
                }
            }

            if (payload["incremental"] is not JsonArray incremental)
            {
                continue;
            }

            foreach (var entry in incremental)
            {
                var target = data;

                foreach (var segment in paths[entry!["id"]!.GetValue<string>()])
                {
                    target = segment!.GetValueKind() is JsonValueKind.Number
                        ? target?.AsArray().ElementAtOrDefault(segment.GetValue<int>())
                        : target?[segment.GetValue<string>()];
                }

                if (target is not JsonObject targetObject)
                {
                    return $"The path of the entry {entry["id"]} does not exist yet.";
                }

                foreach (var (name, value) in entry["data"]!.AsObject())
                {
                    targetObject[name] = value!.DeepClone();
                }
            }
        }

        return data!.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private static ValueTask<IRequestExecutor> CreateExecutorAsync(Gates gates)
    {
        var builder = new ServiceCollection()
            .AddSingleton(gates)
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(options => options.EnableDefer = true);

        builder.Services.AddSingleton<IFactory<OperationContextOwner>>(
            sp => new OperationContextOwnerFactory(
                gates.Wrap(sp.GetRequiredService<ObjectPool<OperationContext>>())));

        return builder.BuildRequestExecutorAsync();
    }

    public sealed class Query
    {
        public async Task<string> GetSlowRootAsync([Service] Gates gates)
        {
            await gates.Root.Task.ConfigureAwait(false);
            return "root";
        }

        public Product[] GetProducts() => [new Product()];
    }

    public sealed class Product
    {
        public string Name => "product";

        public Review[] Reviews => [new Review()];

        public async Task<string> GetSlowProductAsync([Service] Gates gates)
        {
            await gates.Product.Task.ConfigureAwait(false);
            return "slow product";
        }
    }

    public sealed class Review
    {
        public string Body => "body";

        public async Task<string> GetDetailAsync([Service] Gates gates)
        {
            await gates.Review.Task.ConfigureAwait(false);
            return "detail";
        }
    }

    public sealed class Gates
    {
        private readonly List<(int Count, TaskCompletionSource Signal)> _waiters = [];
        private int _settled;

        public TaskCompletionSource Root { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Product { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Review { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WaitForSettledGroupsAsync(int count)
        {
            lock (_waiters)
            {
                if (_settled >= count)
                {
                    return Task.CompletedTask;
                }

                var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _waiters.Add((count, signal));
                return signal.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            }
        }

        private void Settled()
        {
            lock (_waiters)
            {
                _settled++;

                foreach (var waiter in _waiters)
                {
                    if (waiter.Count <= _settled)
                    {
                        waiter.Signal.TrySetResult();
                    }
                }
            }
        }

        internal ObjectPool<OperationContext> Wrap(ObjectPool<OperationContext> inner) => new Pool(this, inner);

        // a deferred group returns its operation context once its result was handed to the coordinator.
        private sealed class Pool(Gates gates, ObjectPool<OperationContext> inner) : ObjectPool<OperationContext>
        {
            public override OperationContext Get() => inner.Get();

            public override void Return(OperationContext obj)
            {
                inner.Return(obj);
                gates.Settled();
            }
        }
    }
}
