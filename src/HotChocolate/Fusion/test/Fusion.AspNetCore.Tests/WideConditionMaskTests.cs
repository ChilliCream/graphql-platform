using System.Text;
using System.Text.Json;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class WideConditionMaskTests : FusionTestBase
{
    [Fact]
    public async Task Executes_Operation_With_More_Than_64_Include_Conditions()
    {
        // arrange
        const int conditionCount = 70;

        using var server = CreateSourceSchema(
            "A",
            """
            type Query {
              foo: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);

        // act: every even condition is included, every odd condition is skipped.
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            CreateIncludeDocument(conditionCount),
            variables: CreateVariables("v", conditionCount, i => i % 2 == 0));

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var operationResult = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

        Assert.Equal(JsonValueKind.Undefined, operationResult.Errors.ValueKind);
        Assert.True(operationResult.Data.TryGetProperty("plain", out _));

        for (var i = 0; i < conditionCount; i++)
        {
            Assert.Equal(i % 2 == 0, operationResult.Data.TryGetProperty($"f{i}", out _));
        }
    }

    [Fact]
    public async Task Include_Condition_Ceiling_Produces_Request_Error()
    {
        // arrange
        using var server = CreateSourceSchema(
            "A",
            """
            type Query {
              foo: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.MaxAllowedIncludeConditions = 4));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            CreateIncludeDocument(conditionCount: 5),
            variables: CreateVariables("v", 5, _ => true));

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var operationResult = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

        Assert.Contains(
            "The operation exceeds the maximum allowed number of include conditions (4).",
            operationResult.Errors.GetRawText());
    }

    [Fact]
    public async Task Defer_Condition_Ceiling_Produces_Request_Error()
    {
        // arrange
        using var server = CreateSourceSchema(
            "A",
            """
            type Query {
              foo: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.MaxAllowedDeferConditions = 4));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            CreateDeferDocument(conditionCount: 5),
            variables: CreateVariables("d", 5, _ => false));

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var operationResult = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

        Assert.Contains(
            "The operation exceeds the maximum allowed number of defer conditions (4).",
            operationResult.Errors.GetRawText());
    }

    [Fact]
    public async Task Executes_Operation_With_More_Than_64_Defer_Conditions()
    {
        // arrange
        const int conditionCount = 66;

        using var server = CreateSourceSchema(
            "A",
            """
            type Query {
              foo: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);

        // act: only condition 65 defers; all other fragments fold into the initial result.
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            CreateDeferDocument(conditionCount),
            variables: CreateVariables("d", conditionCount, i => i == 65));

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert: buffer the body so the payload stream can be read after the raw text.
        var bodyBytes = await result.HttpResponseMessage.Content.ReadAsByteArrayAsync(
            TestContext.Current.CancellationToken);
        var bodyText = Encoding.UTF8.GetString(bodyBytes);

        using var cloneMessage = CloneResponse(result.HttpResponseMessage, bodyBytes);
        using var clone = new GraphQLHttpResponse(cloneMessage);

        var payloadCount = 0;
        JsonElement initialData = default;

        await foreach (var payload in clone.ReadAsResultStreamAsync())
        {
            if (payloadCount == 0)
            {
                initialData = payload.Data.Clone();
            }

            payloadCount++;
            payload.Dispose();
        }

        Assert.True(payloadCount >= 2, "the deferred field must arrive in a later payload");
        Assert.True(initialData.TryGetProperty("plain", out _));

        for (var i = 0; i < conditionCount; i++)
        {
            Assert.Equal(i != 65, initialData.TryGetProperty($"f{i}", out _));
        }

        // the deferred field is delivered incrementally after the initial payload.
        var initialEnd = bodyText.IndexOf("\"hasNext\"", StringComparison.Ordinal);
        Assert.True(initialEnd >= 0);
        Assert.True(
            bodyText.IndexOf("\"f65\"", StringComparison.Ordinal) > initialEnd,
            "f65 must only appear after the initial payload");
    }

    private static HttpResponseMessage CloneResponse(HttpResponseMessage source, byte[] bodyBytes)
    {
        var clone = new HttpResponseMessage(source.StatusCode);

        foreach (var header in source.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        clone.Content = new ByteArrayContent(bodyBytes);

        foreach (var header in source.Content.Headers)
        {
            clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    private static string CreateIncludeDocument(int conditionCount)
    {
        var sourceText = new StringBuilder();
        sourceText.Append("query(");

        for (var i = 0; i < conditionCount; i++)
        {
            sourceText.Append($"$v{i}: Boolean! ");
        }

        sourceText.Append(") { plain: foo");

        for (var i = 0; i < conditionCount; i++)
        {
            sourceText.Append($" f{i}: foo @include(if: $v{i})");
        }

        sourceText.Append(" }");
        return sourceText.ToString();
    }

    private static string CreateDeferDocument(int conditionCount)
    {
        var sourceText = new StringBuilder();
        sourceText.Append("query(");

        for (var i = 0; i < conditionCount; i++)
        {
            sourceText.Append($"$d{i}: Boolean! ");
        }

        sourceText.Append(") { plain: foo");

        for (var i = 0; i < conditionCount; i++)
        {
            sourceText.Append($" ... @defer(if: $d{i}) {{ f{i}: foo }}");
        }

        sourceText.Append(" }");
        return sourceText.ToString();
    }

    private static Dictionary<string, object?> CreateVariables(
        string prefix,
        int count,
        Func<int, bool> value)
    {
        var variables = new Dictionary<string, object?>();

        for (var i = 0; i < count; i++)
        {
            variables.Add($"{prefix}{i}", value(i));
        }

        return variables;
    }
}
