using System.Net;
using System.Text;
using System.Text.Json;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.Execution;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;
using static System.Net.HttpStatusCode;
using static HotChocolate.AspNetCore.HttpTransportVersion;
using VariableBatchRequest = HotChocolate.Transport.VariableBatchRequest;

namespace HotChocolate.Fusion;

public class VariableBatchingTests : FusionTestBase
{
    [Fact]
    public async Task Execute_Should_ProduceAResultPerVariableSet_When_ServerOptionsAreDefault()
    {
        // arrange
        // Several variable sets run as parallel plan executions over the one shared request arena.
        // Each set fetches from the subgraph, so a later set still rents from that arena after an
        // earlier set has completed; a premature seal would make those rentals fail.
        using var serverA = CreateSourceSchema(
            "A",
            r => r.AddQueryType<SourceSchema.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", serverA)
            ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new GraphQLHttpRequest(
            new VariableBatchRequest(
                """
                query testQuery($input: String!) {
                  field(input: $input)
                }
                """,
                variables:
                [
                    new Dictionary<string, object?> { ["input"] = "first" },
                    new Dictionary<string, object?> { ["input"] = "second" },
                    new Dictionary<string, object?> { ["input"] = "third" }
                ]),
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        var values = new List<string>();
        await foreach (var result in response.ReadAsResultStreamAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            using (result)
            {
                Assert.Equal(JsonValueKind.Undefined, result.Errors.ValueKind);
                values.Add(result.Data.GetProperty("field").GetString()!);
            }
        }

        // assert
        Assert.Equal(["first", "second", "third"], [.. values.OrderBy(v => v)]);
    }

    [Fact]
    public async Task Request_Batch_Containing_A_Variable_Batch_Produces_A_Result_Per_Set()
    {
        // arrange
        // A request batch carries a variable batch as one of its items. Each variable set is unwrapped
        // from the inner result batch and forwarded into the request batch response stream, so the
        // wrapper carrying the request arena is disposed once the response stream is disposed.
        using var serverA = CreateSourceSchema(
            "A",
            r => r.AddQueryType<SourceSchema.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", serverA)
            ],
            configureGatewayBuilder: b => b.ModifyServerOptions(o => o.Batching = AllowedBatching.All));

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new GraphQLHttpRequest(
            new OperationBatchRequest(
                [
                    new VariableBatchRequest(
                        """
                        query testQuery($input: String!) {
                          field(input: $input)
                        }
                        """,
                        variables:
                        [
                            new Dictionary<string, object?> { ["input"] = "first" },
                            new Dictionary<string, object?> { ["input"] = "second" },
                            new Dictionary<string, object?> { ["input"] = "third" }
                        ])
                ]),
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        var values = new List<string>();
        await foreach (var result in response.ReadAsResultStreamAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            using (result)
            {
                Assert.Equal(JsonValueKind.Undefined, result.Errors.ValueKind);
                values.Add(result.Data.GetProperty("field").GetString()!);
            }
        }

        // assert
        Assert.Equal(["first", "second", "third"], [.. values.OrderBy(v => v)]);
    }

    [Theory]
    [InlineData("query($input: String!) { field(input: $input) }", Draft20250508, BadRequest)]
    [InlineData(
        "query($input: String!) { field(input: $input) }",
        Draft20260903,
        UnprocessableContent)]
    [InlineData("{ __typename }", Draft20250508, BadRequest)]
    [InlineData("{ __typename }", Draft20260903, UnprocessableContent)]
    public async Task Execute_Should_ReturnRequestError_When_VariableBatchIsEmpty(
        string query,
        HttpTransportVersion transportVersion,
        HttpStatusCode expectedStatusCode)
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            r => r.AddQueryType<SourceSchema.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", serverA)
            ],
            configureGatewayBuilder: b => b.AddHttpResponseFormatter(
                new HttpResponseFormatterOptions
                {
                    HttpTransportVersion = transportVersion
                }));

        using var client = gateway.CreateClient();

        // act
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri("http://localhost:5000/graphql"));
        request.Content = new StringContent(
            $$"""{ "query": "{{query}}", "variables": [] }""",
            Encoding.UTF8,
            "application/json");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                $$$"""
                Headers:
                Vary: Accept
                Content-Type: application/graphql-response+json; charset=utf-8
                -------------------------->
                Status Code: {{{expectedStatusCode}}}
                -------------------------->
                {"errors":[{"message":"A variable batch request must contain at least one variable set.","extensions":{"code":"HC0009"}}]}
                """);
    }

    [Fact]
    public async Task Execute_Should_ReturnInternalServerError_When_CoercedVariablesAreMissing()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            r => r.AddQueryType<SourceSchema.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", serverA)
            ],
            configureGatewayBuilder: b => b.UseRequest(
                next => context =>
                {
                    context.VariableValues = [];
                    return next(context);
                },
                key: "ClearVariableValues",
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware));

        using var client = gateway.CreateClient();

        const string body =
            """
            {
                "query": "query testQuery($input: String!) { field(input: $input) }",
                "variables": [{ "input": "first" }]
            }
            """;

        // act
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri("http://localhost:5000/graphql"));
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                """
                Headers:
                Vary: Accept
                Content-Type: application/graphql-response+json; charset=utf-8
                -------------------------->
                Status Code: InternalServerError
                -------------------------->
                {"errors":[{"message":"Unexpected Execution Error"}]}
                """);
    }

    public static class SourceSchema
    {
        public class Query
        {
            // The first set resolves immediately while the others wait, so the first plan execution
            // completes (and would seal the shared request arena under a premature seal) before the
            // later sets fetch their result and rent into that same arena.
            public async Task<string> GetField(string input)
            {
                if (input != "first")
                {
                    await Task.Delay(250);
                }

                return input;
            }
        }
    }
}
