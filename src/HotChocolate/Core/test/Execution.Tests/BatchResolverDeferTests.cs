using System.Text.Json;
using CookieCrumble;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class BatchResolverDeferTests
{
    [Fact]
    public async Task BatchField_In_Defer_Fragment_Should_Deliver_Data_Without_Errors()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("slowField")
                        .Type<StringType>()
                        .Resolve(async _ =>
                        {
                            await Task.Delay(10);
                            return "slow";
                        });
                    d.Field("productById")
                        .Argument("id", a => a.Type<NonNullType<IntType>>())
                        .Type<ObjectType<DeferProduct>>()
                        .ResolveBatch(contexts =>
                        {
                            var results = new ResolverResult[contexts.Count];

                            for (var i = 0; i < contexts.Count; i++)
                            {
                                var id = contexts[i].ArgumentValue<int>("id");
                                results[i] = ResolverResult.Ok(new DeferProduct(id, $"Product {id}"));
                            }

                            return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                        });
                })
                .AddObjectType<DeferProduct>(d =>
                {
                    d.Field(p => p.Id);
                    d.Field(p => p.Name);
                })
                .ModifyOptions(o => o.EnableDefer = true)
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                slowField
                ... @defer {
                    productById(id: 1) {
                        name
                    }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var (errors, data) = await DrainAsync(result);
        Assert.Empty(errors);
        Assert.Contains("\"name\":\"Product 1\"", data);
    }

    [Fact]
    public async Task RelayNode_In_Defer_Fragment_Should_Deliver_Data_Without_Errors()
    {
        // arrange
        // Mirrors the proven non-deferred NodeResolver_..._Fetch_Through_Node_Field test;
        // the only difference is the @defer wrapper around the batch-only node field.
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithBatchNodeResolver>()
                .AddType<BatchEntity>()
                .AddGlobalObjectIdentification()
                .ModifyOptions(o => o.EnableDefer = true)
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                ... @defer {
                    node(id: "QmF0Y2hFbnRpdHk6YWJj") {
                        ... on BatchEntity {
                            id
                            name
                        }
                    }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var (errors, data) = await DrainAsync(result);
        Assert.Empty(errors);
        Assert.Contains("\"name\":\"abc\"", data);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Defer_Should_Report_Error_In_Owning_Payload_When_Batch_Entry_Fails(bool reportError)
    {
        // arrange
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d =>
            {
                d.Field("immediate").Resolve("ready");
                d.Field("product")
                    .Type<NonNullType<StringType>>()
                    .ResolveBatch(async contexts =>
                    {
                        await release.Task.WaitAsync(TestContext.Current.CancellationToken);

                        if (reportError)
                        {
                            contexts[0].ReportError("Deferred product failed.");
                        }

                        return new[] { ResolverResult.Ok(null) };
                    });
            })
            .ModifyOptions(o => o.EnableDefer = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ immediate ... @defer(label: \"product\") { product } }",
            cancellationToken: TestContext.Current.CancellationToken);
        var payloads = await DrainPayloadsAsync(result, release)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        var snapshot = new Snapshot(postFix: reportError.ToString());

        foreach (var payload in payloads)
        {
            snapshot.Add(payload, "Payload", "json");
        }

        snapshot.MatchMarkdownSnapshot();
    }

    private static async Task<List<string>> DrainPayloadsAsync(
        IExecutionResult result,
        TaskCompletionSource release)
    {
        var payloads = new List<string>();
        var stream = Assert.IsType<ResponseStream>(result);

        await foreach (var response in stream.ReadResultsAsync())
        {
            await using (response)
            {
                payloads.Add(response.ToJson());
                release.TrySetResult();
            }
        }

        return payloads;
    }

    private static async Task<(List<string> errors, string data)> DrainAsync(IExecutionResult result)
    {
        var errors = new List<string>();
        var dataFragments = new List<string>();

        if (result is ResponseStream stream)
        {
            await using (stream)
            {
                await foreach (var response in stream.ReadResultsAsync())
                {
                    Collect(response.ToJson(), errors, dataFragments);
                }
            }
        }
        else
        {
            Collect(result.ToJson(), errors, dataFragments);
        }

        // Concatenate the compacted fragments. Compaction only removes structural whitespace
        // between JSON tokens, so spaces inside string values such as "Product 1" survive and
        // the data assertions remain satisfiable once the deferred payload delivers the field.
        var data = string.Concat(dataFragments);
        return (errors, data);
    }

    private static void Collect(string json, List<string> errors, List<string> dataFragments)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        CollectErrors(root, errors);

        if (root.TryGetProperty("incremental", out var incremental)
            && incremental.ValueKind == JsonValueKind.Array)
        {
            foreach (var part in incremental.EnumerateArray())
            {
                CollectErrors(part, errors);
            }

            dataFragments.Add(Compact(incremental));
        }

        if (root.TryGetProperty("data", out var dataElement))
        {
            dataFragments.Add(Compact(dataElement));
        }
    }

    private static string Compact(JsonElement element)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            element.WriteTo(writer);
        }

        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void CollectErrors(JsonElement element, List<string> errors)
    {
        if (element.TryGetProperty("errors", out var errorsElement)
            && errorsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var error in errorsElement.EnumerateArray())
            {
                errors.Add(error.GetRawText());
            }
        }
    }

    public record DeferProduct(int Id, string Name);

    public class QueryWithBatchNodeResolver
    {
        [Lookup]
        [NodeResolver]
        [BatchResolver]
        public List<BatchEntity> GetBatchEntity(List<string> id)
        {
            var result = new List<BatchEntity>();

            foreach (var value in id)
            {
                result.Add(new BatchEntity { Name = value });
            }

            return result;
        }
    }

    public class BatchEntity
    {
        public string Id
        {
            get => Name;
            set => Name = value;
        }

        public required string Name { get; set; }
    }
}
