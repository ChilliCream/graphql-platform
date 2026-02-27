using System.Collections.Immutable;
using System.IO.Pipelines;
using System.Text.Json;
using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
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
