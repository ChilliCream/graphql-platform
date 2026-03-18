using System.Collections.Immutable;
using System.IO.Pipelines;
using System.Text.Json;
using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Transport.Formatters;

namespace HotChocolate.AspNetCore.Serialization;

public sealed class DeferredResultFormatterTests
{
    private const string Payload1New =
        """{"data":{"product":{"name":"Abc"}},"pending":[{"id":"2","path":["product"],"label":"productDescription"}],"hasNext":true}""";
    private const string Payload2New =
        """{"incremental":[{"id":"2","data":{"description":"Abc desc"}}],"completed":[{"id":"2"}],"hasNext":false}""";
    private const string Payload1Legacy =
        """{"data":{"product":{"name":"Abc"}},"hasNext":true}""";
    private const string Payload2Legacy =
        """{"incremental":[{"data":{"description":"Abc desc"},"path":["product"],"label":"productDescription"}],"hasNext":false}""";

    [Fact]
    public async Task MultiPart_Formats_New_Defer_Structure()
    {
        var content = await FormatAsync(
            new MultiPartResultFormatter(),
            ExecutionResultFormatFlags.None);

        Assert.Equal(
            Normalize(
                $$"""
                ---
                Content-Type: application/json; charset=utf-8

                {{Payload1New}}
                ---
                Content-Type: application/json; charset=utf-8

                {{Payload2New}}
                -----
                """),
            content);
    }

    [Fact]
    public async Task MultiPart_Formats_Legacy_Defer_Structure()
    {
        var content = await FormatAsync(
            new MultiPartResultFormatter(),
            ExecutionResultFormatFlags.IncrementalRfc1);

        Assert.Equal(
            Normalize(
                $$"""
                ---
                Content-Type: application/json; charset=utf-8

                {{Payload1Legacy}}
                ---
                Content-Type: application/json; charset=utf-8

                {{Payload2Legacy}}
                -----
                """),
            content);
    }

    [Fact]
    public async Task EventStream_Formats_New_Defer_Structure()
    {
        var content = await FormatAsync(
            new EventStreamResultFormatter(default),
            ExecutionResultFormatFlags.None);

        Assert.Equal(
            Normalize(
                $$"""
                event: next
                data: {{Payload1New}}

                event: next
                data: {{Payload2New}}

                event: complete
                """),
            content);
    }

    [Fact]
    public async Task EventStream_Formats_Legacy_Defer_Structure()
    {
        var content = await FormatAsync(
            new EventStreamResultFormatter(default),
            ExecutionResultFormatFlags.IncrementalRfc1);

        Assert.Equal(
            Normalize(
                $$"""
                event: next
                data: {{Payload1Legacy}}

                event: next
                data: {{Payload2Legacy}}

                event: complete
                """),
            content);
    }

    [Fact]
    public async Task JsonLines_Formats_New_Defer_Structure()
    {
        var content = await FormatAsync(
            new JsonLinesResultFormatter(default),
            ExecutionResultFormatFlags.None);

        Assert.Equal(
            Normalize(
                $$"""
                {{Payload1New}}
                {{Payload2New}}
                """),
            content);
    }

    [Fact]
    public async Task JsonLines_Formats_Legacy_Defer_Structure()
    {
        var content = await FormatAsync(
            new JsonLinesResultFormatter(default),
            ExecutionResultFormatFlags.IncrementalRfc1);

        Assert.Equal(
            Normalize(
                $$"""
                {{Payload1Legacy}}
                {{Payload2Legacy}}
                """),
            content);
    }

    [Fact]
    public async Task Legacy_Merges_Parent_And_Defer_Overlap()
    {
        var document = ParseDocument(
            """
            {
                product {
                    name
                    description
                }
                ... @defer(label: "foo") {
                    product {
                        name
                        description
                        reviews {
                            rating
                        }
                    }
                }
            }
            """);

        var initial = CreateInitialResult(
            document,
            new Dictionary<string, object?>
            {
                ["product"] = new Dictionary<string, object?>
                {
                    ["name"] = "Abc",
                    ["description"] = "Abc desc"
                }
            },
            hasNext: true,
            new PendingResult(2, Path.Root, "foo"));

        var incremental = CreateIncrementalEnvelope(
            document,
            new IncrementalObjectResult(
                2,
                data: CreateData(
                    new Dictionary<string, object?>
                    {
                        ["product"] = new Dictionary<string, object?>
                        {
                            ["reviews"] = new[]
                            {
                                new Dictionary<string, object?>
                                {
                                    ["rating"] = 5
                                }
                            }
                        }
                    })),
            hasNext: false,
            completedId: 2);

        var lines = await FormatLegacyJsonLinesAsync(initial, incremental);

        Assert.Equal(
            [
                """{"data":{"product":{"name":"Abc","description":"Abc desc"}},"hasNext":true}""",
                """{"incremental":[{"data":{"product":{"name":"Abc","description":"Abc desc","reviews":[{"rating":5}]}},"path":[],"label":"foo"}],"hasNext":false}"""
            ],
            lines);
    }

    [Fact]
    public async Task Legacy_Merges_Two_Defers_With_Same_Fields()
    {
        var document = ParseDocument(
            """
            {
                ... @defer(label: "a") {
                    product {
                        name
                    }
                }
                ... @defer(label: "b") {
                    product {
                        name
                    }
                }
            }
            """);

        var initial = CreateInitialResult(
            document,
            new Dictionary<string, object?>(),
            hasNext: true,
            new PendingResult(2, Path.Root, "a"),
            new PendingResult(3, Path.Root, "b"));

        var incrementalA = CreateIncrementalEnvelope(
            document,
            new IncrementalObjectResult(
                2,
                data: CreateData(
                    new Dictionary<string, object?>
                    {
                        ["product"] = new Dictionary<string, object?>
                        {
                            ["name"] = "Abc"
                        }
                    })),
            hasNext: true,
            completedId: 2);

        var incrementalB = CreateIncrementalEnvelope(
            document,
            new IncrementalObjectResult(
                3,
                data: CreateData(
                    new Dictionary<string, object?>
                    {
                        ["product"] = new Dictionary<string, object?>()
                    })),
            hasNext: false,
            completedId: 3);

        var lines = await FormatLegacyJsonLinesAsync(initial, incrementalA, incrementalB);

        Assert.Equal(
            new[]
            {
                """{"data":{},"hasNext":true}""",
                """{"incremental":[{"data":{"product":{"name":"Abc"}},"path":[],"label":"a"}],"hasNext":true}""",
                """{"incremental":[{"data":{"product":{"name":"Abc"}},"path":[],"label":"b"}],"hasNext":false}"""
            },
            lines);
    }

    [Fact]
    public async Task Legacy_Merges_Nested_Defers()
    {
        var document = ParseDocument(
            """
            {
                ... @defer(label: "outer") {
                    product {
                        name
                        ... @defer(label: "inner") {
                            name
                            reviews {
                                rating
                            }
                        }
                    }
                }
            }
            """);

        var initial = CreateInitialResult(
            document,
            new Dictionary<string, object?>(),
            hasNext: true,
            new PendingResult(2, Path.Root, "outer"));

        var outer = CreateIncrementalEnvelope(
            document,
            new IncrementalObjectResult(
                2,
                data: CreateData(
                    new Dictionary<string, object?>
                    {
                        ["product"] = new Dictionary<string, object?>
                        {
                            ["name"] = "Abc"
                        }
                    })),
            hasNext: true,
            completedId: 2,
            new PendingResult(3, Path.Root.Append("product"), "inner"));

        var inner = CreateIncrementalEnvelope(
            document,
            new IncrementalObjectResult(
                3,
                data: CreateData(
                    new Dictionary<string, object?>
                    {
                        ["reviews"] = new[]
                        {
                            new Dictionary<string, object?>
                            {
                                ["rating"] = 5
                            }
                        }
                    })),
            hasNext: false,
            completedId: 3);

        var lines = await FormatLegacyJsonLinesAsync(initial, outer, inner);

        Assert.Equal(
            new[]
            {
                """{"data":{},"hasNext":true}""",
                """{"incremental":[{"data":{"product":{"name":"Abc"}},"path":[],"label":"outer"}],"hasNext":true}""",
                """{"incremental":[{"data":{"name":"Abc","reviews":[{"rating":5}]},"path":["product"],"label":"inner"}],"hasNext":false}"""
            },
            lines);
    }

    [Fact]
    public async Task Legacy_Merges_Deep_Object_Overlap()
    {
        var document = ParseDocument(
            """
            {
                product {
                    details {
                        sku
                    }
                }
                ... @defer(label: "foo") {
                    product {
                        details {
                            sku
                            weight
                        }
                    }
                }
            }
            """);

        var initial = CreateInitialResult(
            document,
            new Dictionary<string, object?>
            {
                ["product"] = new Dictionary<string, object?>
                {
                    ["details"] = new Dictionary<string, object?>
                    {
                        ["sku"] = "SKU-1"
                    }
                }
            },
            hasNext: true,
            new PendingResult(2, Path.Root, "foo"));

        var incremental = CreateIncrementalEnvelope(
            document,
            new IncrementalObjectResult(
                2,
                data: CreateData(
                    new Dictionary<string, object?>
                    {
                        ["product"] = new Dictionary<string, object?>
                        {
                            ["details"] = new Dictionary<string, object?>
                            {
                                ["weight"] = 10
                            }
                        }
                    })),
            hasNext: false,
            completedId: 2);

        var lines = await FormatLegacyJsonLinesAsync(initial, incremental);

        Assert.Equal(
            new[]
            {
                """{"data":{"product":{"details":{"sku":"SKU-1"}}},"hasNext":true}""",
                """{"incremental":[{"data":{"product":{"details":{"sku":"SKU-1","weight":10}}},"path":[],"label":"foo"}],"hasNext":false}"""
            },
            lines);
    }

    [Fact]
    public async Task Legacy_Rebases_Deduplicated_SubPath_To_Defer_Path()
    {
        var document = ParseDocument(
            """
            {
                stage {
                    metrics {
                        operations {
                            __typename
                        }
                    }
                    ... @defer(label: "foo") {
                        metrics {
                            operations {
                                totalCount
                            }
                        }
                        id
                    }
                }
            }
            """);

        var initial = CreateInitialResult(
            document,
            new Dictionary<string, object?>
            {
                ["stage"] = new Dictionary<string, object?>
                {
                    ["metrics"] = new Dictionary<string, object?>
                    {
                        ["operations"] = new Dictionary<string, object?>()
                    },
                    ["id"] = "1"
                }
            },
            hasNext: true,
            new PendingResult(2, Path.Root.Append("stage"), "foo"));

        var incremental = CreateIncrementalEnvelope(
            document,
            new IncrementalObjectResult(
                2,
                subPath: Path.Root.Append("metrics"),
                data: CreateData(
                    new Dictionary<string, object?>
                    {
                        ["operations"] = new Dictionary<string, object?>
                        {
                            ["totalCount"] = 5
                        }
                    })),
            hasNext: false,
            completedId: 2);

        var lines = await FormatLegacyJsonLinesAsync(initial, incremental);

        Assert.Equal(
            new[]
            {
                """{"data":{"stage":{"metrics":{"operations":{}},"id":"1"}},"hasNext":true}""",
                """{"incremental":[{"data":{"metrics":{"operations":{"totalCount":5}},"id":"1"},"path":["stage"],"label":"foo"}],"hasNext":false}"""
            },
            lines);
    }

    [Fact]
    public async Task Legacy_Duplicates_Null_Values()
    {
        var document = ParseDocument(
            """
            {
                product {
                    name
                    description
                }
                ... @defer(label: "foo") {
                    product {
                        name
                        description
                        reviews {
                            rating
                        }
                    }
                }
            }
            """);

        var initial = CreateInitialResult(
            document,
            new Dictionary<string, object?>
            {
                ["product"] = new Dictionary<string, object?>
                {
                    ["name"] = "Abc",
                    ["description"] = null
                }
            },
            hasNext: true,
            new PendingResult(2, Path.Root, "foo"));

        var incremental = CreateIncrementalEnvelope(
            document,
            new IncrementalObjectResult(
                2,
                data: CreateData(
                    new Dictionary<string, object?>
                    {
                        ["product"] = new Dictionary<string, object?>
                        {
                            ["reviews"] = new[]
                            {
                                new Dictionary<string, object?>
                                {
                                    ["rating"] = 5
                                }
                            }
                        }
                    })),
            hasNext: false,
            completedId: 2);

        var lines = await FormatLegacyJsonLinesAsync(initial, incremental);

        Assert.Equal(
            new[]
            {
                """{"data":{"product":{"name":"Abc","description":null}},"hasNext":true}""",
                """{"incremental":[{"data":{"product":{"name":"Abc","description":null,"reviews":[{"rating":5}]}},"path":[],"label":"foo"}],"hasNext":false}"""
            },
            lines);
    }

    [Fact]
    public async Task Legacy_No_Overlap_Behavior_Is_Unchanged()
    {
        var document = ParseDocument(
            """
            {
                product {
                    name
                }
                ... @defer(label: "foo") {
                    product {
                        reviews {
                            rating
                        }
                    }
                }
            }
            """);

        var initial = CreateInitialResult(
            document,
            new Dictionary<string, object?>
            {
                ["product"] = new Dictionary<string, object?>
                {
                    ["name"] = "Abc"
                }
            },
            hasNext: true,
            new PendingResult(2, Path.Root, "foo"));

        var incremental = CreateIncrementalEnvelope(
            document,
            new IncrementalObjectResult(
                2,
                data: CreateData(
                    new Dictionary<string, object?>
                    {
                        ["product"] = new Dictionary<string, object?>
                        {
                            ["reviews"] = new[]
                            {
                                new Dictionary<string, object?>
                                {
                                    ["rating"] = 5
                                }
                            }
                        }
                    })),
            hasNext: false,
            completedId: 2);

        var lines = await FormatLegacyJsonLinesAsync(initial, incremental);

        Assert.Equal(
            new[]
            {
                """{"data":{"product":{"name":"Abc"}},"hasNext":true}""",
                """{"incremental":[{"data":{"product":{"reviews":[{"rating":5}]}},"path":[],"label":"foo"}],"hasNext":false}"""
            },
            lines);
    }

    private static async Task<string> FormatAsync(
        IExecutionResultFormatter formatter,
        ExecutionResultFormatFlags flags)
    {
        await using var output = new MemoryStream();
        var writer = PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true));
        var stream = CreateDeferredResponseStream();

        await formatter.FormatAsync(stream, writer, flags, CancellationToken.None);
        await writer.CompleteAsync();

        output.Position = 0;
        return Normalize(await new StreamReader(output).ReadToEndAsync());
    }

    private static async Task<IReadOnlyList<string>> FormatLegacyJsonLinesAsync(params OperationResult[] results)
    {
        await using var output = new MemoryStream();
        var writer = PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true));
        var formatter = new JsonLinesResultFormatter(default);
        var stream = new ResponseStream(
            () => CreateResults(results),
            ExecutionResultKind.DeferredResult);

        await formatter.FormatAsync(
            stream,
            writer,
            ExecutionResultFormatFlags.IncrementalRfc1,
            CancellationToken.None);
        await writer.CompleteAsync();

        output.Position = 0;
        var content = await new StreamReader(output).ReadToEndAsync();
        return content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static IResponseStream CreateDeferredResponseStream()
        => new ResponseStream(
            CreateResults,
            ExecutionResultKind.DeferredResult);

    private static async IAsyncEnumerable<OperationResult> CreateResults()
    {
        yield return CreateInitialResult();
        await Task.Yield();
        yield return CreateIncrementalResult();
    }

    private static async IAsyncEnumerable<OperationResult> CreateResults(IReadOnlyList<OperationResult> results)
    {
        for (var i = 0; i < results.Count; i++)
        {
            yield return results[i];
            await Task.Yield();
        }
    }

    private static OperationResult CreateInitialResult()
    {
        var result = new OperationResult(
            CreateData(
                new Dictionary<string, object?>
                {
                    ["product"] = new Dictionary<string, object?>
                    {
                        ["name"] = "Abc"
                    }
                }));

        result.Pending = ImmutableList<PendingResult>.Empty.Add(
            new PendingResult(2, Path.Root.Append("product"), "productDescription"));
        result.HasNext = true;
        return result;
    }

    private static OperationResult CreateIncrementalResult()
    {
        var result = new OperationResult(ImmutableOrderedDictionary<string, object?>.Empty.Add("__placeholder", true))
        {
            Incremental = ImmutableList<IIncrementalResult>.Empty.Add(
                new IncrementalObjectResult(
                    2,
                    data: CreateData(
                        new Dictionary<string, object?>
                        {
                            ["description"] = "Abc desc"
                        }))),
            Completed = ImmutableList<CompletedResult>.Empty.Add(new CompletedResult(2)),
            HasNext = false,
            Extensions = []
        };

        return result;
    }

    private static OperationResult CreateInitialResult(
        DocumentNode document,
        object data,
        bool hasNext,
        params PendingResult[] pending)
    {
        var result = new OperationResult(CreateData(data))
        {
            Document = document,
            HasNext = hasNext
        };

        if (pending.Length > 0)
        {
            result.Pending = ImmutableList.Create(pending);
        }

        return result;
    }

    private static OperationResult CreateIncrementalEnvelope(
        DocumentNode document,
        IIncrementalResult incremental,
        bool hasNext,
        int? completedId = null,
        params PendingResult[] pending)
    {
        var result = new OperationResult(ImmutableOrderedDictionary<string, object?>.Empty.Add("__placeholder", true))
        {
            Document = document,
            Incremental = ImmutableList<IIncrementalResult>.Empty.Add(incremental),
            HasNext = hasNext,
            Extensions = []
        };

        if (completedId.HasValue)
        {
            result.Completed = ImmutableList<CompletedResult>.Empty.Add(new CompletedResult(completedId.Value));
        }

        if (pending.Length > 0)
        {
            result.Pending = ImmutableList.Create(pending);
        }

        return result;
    }

    private static DocumentNode ParseDocument(string query)
        => Utf8GraphQLParser.Parse(query);

    private static OperationResultData CreateData(object value)
        => new(value, isValueNull: false, new DictionaryJsonFormatter(value), memoryHolder: null);

    private static string Normalize(string value)
        => value.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

    private sealed class DictionaryJsonFormatter(object value) : IRawJsonFormatter
    {
        private static readonly JsonSerializerOptions s_options = new(JsonSerializerDefaults.Web);

        public void WriteDataTo(JsonWriter jsonWriter)
            => JsonValueFormatter.WriteValue(jsonWriter, value, s_options);
    }
}
