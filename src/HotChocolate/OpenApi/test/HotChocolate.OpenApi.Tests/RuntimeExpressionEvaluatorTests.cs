using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.OpenApi.Expressions;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi.Tests;

public sealed class RuntimeExpressionEvaluatorTests
{
    [Theory]
    [MemberData(nameof(Expressions))]
    public void EvaluateExpression_Default_ReturnsExpectedResult(
        RuntimeExpression expression,
        OpenApiParameter parameter,
        object? result)
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost/");

        request.Content = new StringContent(
            """{ "example": 123 }""",
            new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        request.Headers.Add("ExampleRequestHeader", "example-request-header-value");

        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("ExampleResponseHeader", "example-response-header-value");

        using var responseBody = JsonDocument.Parse("""{ "example": 123 }""");

        // Act & Assert
        Assert.Equal(
            result,
            RuntimeExpressionEvaluator.EvaluateExpression(
                expression,
                parameter,
                request,
                pathParameters: new Dictionary<string, object?>() { { "pathParameter", 123 } },
                queryParameters: new Dictionary<string, object?>() { { "queryParameter", 123 } },
                response,
                responseBody.RootElement));
    }

    public static TheoryData<RuntimeExpression, OpenApiParameter, object?> Expressions()
    {
        return new TheoryData<RuntimeExpression, OpenApiParameter, object?>
        {
            {
                RuntimeExpression.Build("$method"),
                new OpenApiParameter(),
                "POST"
            },
            {
                RuntimeExpression.Build("$request.body#/example"),
                new OpenApiParameter()
                {
                    Schema = typeof(int).MapTypeToOpenApiPrimitiveType(),
                },
                123
            },
            {
                RuntimeExpression.Build("$request.header.ExampleRequestHeader"),
                new OpenApiParameter(),
                "example-request-header-value"
            },
            {
                RuntimeExpression.Build("$request.path.pathParameter"),
                new OpenApiParameter()
                {
                    Schema = typeof(int).MapTypeToOpenApiPrimitiveType(),
                },
                123
            },
            {
                RuntimeExpression.Build("$request.query.queryParameter"),
                new OpenApiParameter()
                {
                    Schema = typeof(int).MapTypeToOpenApiPrimitiveType(),
                },
                123
            },
            {
                RuntimeExpression.Build("$response.body#/example"),
                new OpenApiParameter()
                {
                    Schema = typeof(int).MapTypeToOpenApiPrimitiveType(),
                },
                123
            },
            {
                RuntimeExpression.Build("$response.header.ExampleResponseHeader"),
                new OpenApiParameter(),
                "example-response-header-value"
            },
            {
                RuntimeExpression.Build("$statusCode"),
                new OpenApiParameter(),
                200
            },
            {
                RuntimeExpression.Build("$url"),
                new OpenApiParameter(),
                "https://localhost/"
            },
        };
    }
}
