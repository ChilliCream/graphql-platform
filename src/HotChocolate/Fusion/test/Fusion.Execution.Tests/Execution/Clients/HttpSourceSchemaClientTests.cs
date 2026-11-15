using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class HttpSourceSchemaClientTests : SourceSchemaClientTestBase
{
    [Fact]
    public async Task ExecuteBatchAsync_Should_SendFile_When_UploadBatchHasOneOperation()
    {
        // arrange
        await using var fixture = await SourceSchemaClientTestFixture.CreateAsync();
        var handler = new RecordingHandler();
        await using var client = new HttpSourceSchemaClient(
            new DefaultGraphQLHttpClient(new HttpClient(handler), disposeInnerClient: true),
            new HttpSourceSchemaClientConfiguration("A", new Uri("http://localhost:5000/graphql")));
        using var memory = new ChunkedArrayWriter();
        var context = fixture.CreateContext();
        context.RequestContext.Features.Set<IFileLookup>(
            new SingleFileLookup(new TextFile("test.txt", "abc")));
        var requests = ImmutableArray.Create(CreateUploadRequest(fixture.RootNode, memory));

        // act
        await foreach (var batchResult in client.ExecuteBatchAsync(
            context,
            requests,
            TestContext.Current.CancellationToken))
        {
            batchResult.Result.Dispose();
        }

        // assert
        handler.Parts.MatchInlineSnapshots(
        [
            """operations: {"query":"query { field }","variables":{"file":null}}""",
            """map: {"0":["variables.file"]}""",
            "0: abc"
        ]);
    }

    private static SourceSchemaClientRequest CreateUploadRequest(
        ExecutionNode node,
        ChunkedArrayWriter memory)
    {
        var sourceText = "query { field }"u8.ToArray();
        var variables = """{"file":"$.file(0)"}"""u8;
        var start = memory.Position;
        variables.CopyTo(memory.GetSpan(variables.Length));
        memory.Advance(variables.Length);
        var values = JsonSegment.Create(memory.GetSequence(start, memory.Position - start));

        return new SourceSchemaClientRequest
        {
            Node = node,
            SchemaName = "A",
            OperationType = OperationType.Query,
            OperationSourceText = new OperationSourceText(
                "Op",
                OperationType.Query,
                sourceText,
                OperationSourceTextHash.Compute(sourceText)),
            OperationDocument = Utf8GraphQLOperationParser.Parse(sourceText),
            Variables = [new VariableValues(new CompactPath([1, ~0]), values)],
            RequiresFileUpload = true
        };
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string> Parts { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content is MultipartFormDataContent form)
            {
                foreach (var part in form)
                {
                    var name = part.Headers.ContentDisposition?.Name?.Trim('"');
                    var content = await part.ReadAsStringAsync(cancellationToken);
                    Parts.Add($"{name}: {content}");
                }
            }
            else if (request.Content is not null)
            {
                var content = await request.Content.ReadAsStringAsync(cancellationToken);
                Parts.Add($"body: {content}");
            }

            var responseContent = new StringContent("""{"data":{"field":"a"}}""");
            responseContent.Headers.ContentType =
                new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent };
        }
    }

    private sealed class SingleFileLookup(IFile file) : IFileLookup
    {
        public bool TryGetFile(string name, [NotNullWhen(true)] out IFile? result)
        {
            result = name == "0" ? file : null;
            return result is not null;
        }
    }

    private sealed class TextFile(string name, string content) : IFile
    {
        public string Name => name;

        public long? Length => Encoding.UTF8.GetByteCount(content);

        public string? ContentType => "text/plain";

        public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {
            await using var stream = OpenReadStream();
            await stream.CopyToAsync(target, cancellationToken);
        }

        public Stream OpenReadStream() => new MemoryStream(Encoding.UTF8.GetBytes(content));
    }
}
