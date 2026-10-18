using System.Collections.Immutable;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Transport;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Fusion.Transport.Serialization;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients;

public class SourceSchemaRequestBodyTests : FusionTestBase
{
    [Fact]
    public async Task SendAsync_Should_WriteSameBody_When_K6QueryLiteralIsCached()
    {
        // arrange
        var schema = ComposeSchema(
            FileResource.Open("k6-accounts.graphqls"),
            FileResource.Open("k6-inventory.graphqls"),
            FileResource.Open("k6-products.graphqls"),
            FileResource.Open("k6-reviews.graphqls"));
        var plan = PlanOperation(schema, FileResource.Open("k6.graphql"));
        var operations = CollectOperations(plan);
        using var variableWriter = new ChunkedArrayWriter();
        var handler = new CapturingHandler();
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));
        var escapedBodies = new List<string>();
        var cachedBodies = new List<string>();
        var snapshot = Snapshot.Create();

        // act
        foreach (var (id, sourceText, requirements) in operations)
        {
            var variables = CreateVariableValues(variableWriter, requirements, 1);

            escapedBodies.Add(
                await SendAsync(
                    client,
                    handler,
                    new OperationRequest(sourceText.Value, null, null, null, variables, JsonSegment.Empty)));

            var cachedBody = await SendAsync(
                client,
                handler,
                new OperationRequest(sourceText.Value, null, null, null, variables, JsonSegment.Empty)
                {
                    QueryJsonLiteral = sourceText.JsonLiteral
                });
            cachedBodies.Add(cachedBody);
            snapshot.Add(
                cachedBody,
                $"Node {id}, {sourceText.Name} ({sourceText.Value.Length} bytes of operation text)",
                MarkdownLanguages.Json);
        }

        // assert
        Assert.Equal(7, operations.Count);
        Assert.Equal(escapedBodies, cachedBodies);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task SendAsync_Should_WriteSameVariableBatchBody_When_QueryLiteralIsCached()
    {
        // arrange
        var sourceText = CreateSourceText(
            """
            query Lookup($__fusion_1_upc: ID!) {
              product(upc: $__fusion_1_upc) {
                name
              }
            }
            """);
        using var variableWriter = new ChunkedArrayWriter();
        var variables = ImmutableArray.Create(
            CreateVariableValues(variableWriter, [("__fusion_1_upc", "1")]),
            CreateVariableValues(variableWriter, [("__fusion_1_upc", "2")]));
        var handler = new CapturingHandler();
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        // act
        var escapedBody = await SendAsync(
            client,
            handler,
            new VariableBatchRequest(sourceText.Value, null, null, null, variables, JsonSegment.Empty));
        var cachedBody = await SendAsync(
            client,
            handler,
            new VariableBatchRequest(sourceText.Value, null, null, null, variables, JsonSegment.Empty)
            {
                QueryJsonLiteral = sourceText.JsonLiteral
            });

        // assert
        Assert.Equal(escapedBody, cachedBody);
        cachedBody.MatchInlineSnapshot(
            "{\"query\":\"query Lookup($__fusion_1_upc: ID!) {\\n  product(upc: $__fusion_1_upc) {"
            + "\\n    name\\n  }\\n}\","
            + "\"variables\":[{\"__fusion_1_upc\":\"1\"},{\"__fusion_1_upc\":\"2\"}]}");
    }

    [Fact]
    public async Task SendAsync_Should_WriteSameBody_When_QueryContainsEscapableCharacters()
    {
        // arrange
        var sourceText = CreateSourceText(
            """
            query Special($value: String = "quote \" backslash \\ tab 	 non-ascii ü 日本 😀 html <>&'+") {
              field(value: $value)
            }
            """);
        var handler = new CapturingHandler();
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        // act
        var escapedBody = await SendAsync(
            client,
            handler,
            new OperationRequest(sourceText.Value, null, null, null, VariableValues.Empty, JsonSegment.Empty));
        var cachedBody = await SendAsync(
            client,
            handler,
            new OperationRequest(sourceText.Value, null, null, null, VariableValues.Empty, JsonSegment.Empty)
            {
                QueryJsonLiteral = sourceText.JsonLiteral
            });

        // assert
        Assert.Equal(escapedBody, cachedBody);
        // The relaxed encoder keeps non-ASCII characters of the basic plane and escapes surrogate pairs.
        cachedBody.MatchInlineSnapshot(
            "{\"query\":\"query Special($value: String = \\\"quote \\\\\\\" backslash \\\\\\\\ tab \\t "
            + "non-ascii ü 日本 \\uD83D\\uDE00 html <>&'+\\\") {\\n  field(value: $value)\\n}\"}");
    }

    [Fact]
    public void WriteTo_Should_EscapeQuery_When_WriterUsesAnotherEncoder()
    {
        // arrange
        var sourceText = CreateSourceText("{ field(value: \"ü\") }");
        var options = new JsonWriterOptions { Encoder = JavaScriptEncoder.Default };
        var request = new OperationRequest(sourceText.Value, null, null, null, VariableValues.Empty, JsonSegment.Empty)
        {
            QueryJsonLiteral = sourceText.JsonLiteral
        };

        // act
        using var buffer = new PooledArrayWriter();
        request.WriteTo(new JsonWriter(buffer, options));
        var body = Encoding.UTF8.GetString(buffer.WrittenSpan);

        // assert
        body.MatchInlineSnapshot("{\"query\":\"{ field(value: \\u0022\\u00FC\\u0022) }\"}");
    }

    [Fact]
    public void JsonLiteral_Should_BeTheEscapedQuotedValue_When_SourceTextIsCreated()
    {
        // arrange
        var sourceText = CreateSourceText(
            """
            query Test {
              field(value: "ü \" \\")
            }
            """);

        // act
        var literal = Encoding.UTF8.GetString(sourceText.JsonLiteral.Span);

        // assert
        literal.MatchInlineSnapshot(
            """
            "query Test {\n  field(value: \"ü \\\" \\\\\")\n}"
            """);
    }

    [Fact]
    public void Equals_Should_IgnoreJsonLiteral_When_NameTypeValueAndHashAreEqual()
    {
        // arrange
        var bytes = "{ field }"u8.ToArray();
        var hash = OperationSourceTextHash.Compute(bytes);
        var first = new OperationSourceText("Test", OperationType.Query, bytes, hash);
        var second = new OperationSourceText("Test", OperationType.Query, bytes, hash);

        // act
        var equal = first.Equals(second);

        // assert
        Assert.True(equal);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Equal(
            "OperationSourceText { Name = Test, Type = Query, Value = System.ReadOnlyMemory<Byte>[9], Hash = "
            + hash
            + " }",
            first.ToString());
    }

    [Fact]
    public void JsonLiteral_Should_BeEmpty_When_SourceTextIsDefault()
    {
        // arrange
        var sourceText = default(OperationSourceText);

        // act
        var literal = sourceText.JsonLiteral;

        // assert
        Assert.True(literal.IsEmpty);
    }

    private static OperationSourceText CreateSourceText(string operationText)
    {
        var bytes = Encoding.UTF8.GetBytes(operationText);
        return new OperationSourceText("Test", OperationType.Query, bytes, OperationSourceTextHash.Compute(bytes));
    }

    private static List<(int Id, OperationSourceText SourceText, string[] Requirements)> CollectOperations(
        OperationPlan plan)
    {
        var operations = new List<(int, OperationSourceText, string[])>();

        foreach (var node in plan.AllNodes)
        {
            switch (node)
            {
                case OperationExecutionNode operationNode:
                    operations.Add((
                        operationNode.Id,
                        operationNode.Operation,
                        operationNode.Requirements.ToArray().Select(r => r.Key).ToArray()));
                    break;

                case OperationBatchExecutionNode batchNode:
                    foreach (var operation in batchNode.Operations)
                    {
                        operations.Add((
                            batchNode.Id,
                            operation.SourceText,
                            operation.Requirements.ToArray().Select(r => r.Key).ToArray()));
                    }

                    break;
            }
        }

        return operations;
    }

    private static VariableValues CreateVariableValues(
        ChunkedArrayWriter variableWriter,
        string[] requirements,
        int value)
        => CreateVariableValues(
            variableWriter,
            requirements.Select(name => (name, value.ToString())).ToArray());

    private static VariableValues CreateVariableValues(
        ChunkedArrayWriter variableWriter,
        (string Name, string Value)[] variables)
    {
        if (variables.Length == 0)
        {
            return VariableValues.Empty;
        }

        var start = variableWriter.Position;
        using var jsonWriter = new Utf8JsonWriter(variableWriter);
        jsonWriter.WriteStartObject();

        foreach (var (name, value) in variables)
        {
            jsonWriter.WriteString(name, value);
        }

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        return new VariableValues(default, JsonSegment.Create(variableWriter, start, variableWriter.Position - start));
    }

    private static async Task<string> SendAsync(
        DefaultGraphQLHttpClient client,
        CapturingHandler handler,
        IRequestBody body)
    {
        var request = new GraphQLHttpRequest(body, new Uri("http://localhost:5000/graphql"));
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        return Encoding.UTF8.GetString(handler.LastBody ?? []);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public byte[]? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastBody = request.Content is null
                ? null
                : await request.Content.ReadAsByteArrayAsync(cancellationToken);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":{}}""", Encoding.UTF8, "application/graphql-response+json")
            };
            return response;
        }
    }
}
