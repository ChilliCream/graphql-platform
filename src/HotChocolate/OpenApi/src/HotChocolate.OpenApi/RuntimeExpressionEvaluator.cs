using System.Net.Mime;
using System.Text.Json;
using HotChocolate.OpenApi.Exceptions;
using Json.Pointer;
using Microsoft.OpenApi.Expressions;
using Microsoft.OpenApi.Models;
using static HotChocolate.OpenApi.OpenApiResources;

namespace HotChocolate.OpenApi;

internal static class RuntimeExpressionEvaluator
{
    public static object? EvaluateExpression(
        RuntimeExpression expression,
        OpenApiParameter parameter,
        HttpRequestMessage request,
        IReadOnlyDictionary<string, object?> pathParameters,
        IReadOnlyDictionary<string, object?> queryParameters,
        HttpResponseMessage response,
        JsonElement responseBody)
    {
        switch (expression)
        {
            case MethodExpression:
                return request.Method.ToString();

            case RequestExpression requestExpression:
                switch (requestExpression.Source)
                {
                    case BodyExpression bodyExpression:
                        if (request.Content is not null)
                        {
                            var requestBody = JsonSerializer.Deserialize<JsonElement>(
                                request.Content.ReadAsStream());

                            var jsonPointer = JsonPointer.Parse(bodyExpression.Fragment);
                            var jsonElement = jsonPointer.Evaluate(requestBody);

                            if (jsonElement is null)
                            {
                                throw new ExpressionEvaluationException(
                                    string.Format(
                                        RuntimeExpressionEvaluator_JsonPointerPathDoesNotExist,
                                        expression));
                            }

                            return jsonElement.Value.Deserialize(GetParameterType(parameter));
                        }

                        throw new ExpressionEvaluationException(
                            string.Format(
                                RuntimeExpressionEvaluator_MissingRequestBody,
                                expression));

                    case HeaderExpression headerExpression:
                        if (request.Headers.TryGetValues(headerExpression.Token, out var values))
                        {
                            // https://spec.openapis.org/oas/v3.1.0#examples-0
                            // "Single header values only are available"
                            return values.First();
                        }

                        throw new ExpressionEvaluationException(
                            string.Format(
                                RuntimeExpressionEvaluator_MissingRequestHeader,
                                expression));

                    case PathExpression pathExpression:
                        if (pathParameters.TryGetValue(pathExpression.Name, out var pathValue))
                        {
                            return pathValue;
                        }

                        throw new ExpressionEvaluationException(
                            string.Format(
                                RuntimeExpressionEvaluator_MissingPathParameter,
                                expression));

                    case QueryExpression queryExpression:
                        if (queryParameters.TryGetValue(queryExpression.Name, out var queryValue))
                        {
                            return queryValue;
                        }

                        throw new ExpressionEvaluationException(
                            string.Format(
                                RuntimeExpressionEvaluator_MissingQueryParameter,
                                expression));
                    default:
                        throw new InvalidOperationException();
                }

            case ResponseExpression responseExpression:
                switch (responseExpression.Source)
                {
                    case BodyExpression bodyExpression:
                        var jsonPointer = JsonPointer.Parse(bodyExpression.Fragment);
                        var jsonElement = jsonPointer.Evaluate(responseBody);

                        if (jsonElement is null)
                        {
                            throw new ExpressionEvaluationException(
                                string.Format(
                                    RuntimeExpressionEvaluator_JsonPointerPathDoesNotExist,
                                    expression));
                        }

                        return jsonElement.Value.Deserialize(GetParameterType(parameter));

                    case HeaderExpression headerExpression:
                        if (response.Headers.TryGetValues(headerExpression.Token, out var values))
                        {
                            // https://spec.openapis.org/oas/v3.1.0#examples-0
                            // "Single header values only are available"
                            return values.First();
                        }

                        throw new ExpressionEvaluationException(
                            string.Format(
                                RuntimeExpressionEvaluator_MissingResponseHeader,
                                expression));

                    default:
                        throw new InvalidOperationException();
                }

            case StatusCodeExpression:
                return (int)response.StatusCode;

            case UrlExpression:
                return request.RequestUri?.ToString();

            default:
                throw new InvalidOperationException();
        }
    }

    private static Type GetParameterType(OpenApiParameter parameter)
    {
        if (parameter.Content is not null &&
            parameter.Content.TryGetValue(
                MediaTypeNames.Application.Json,
                out var openApiMediaType))
        {
            return openApiMediaType.Schema.MapOpenApiPrimitiveTypeToSimpleType();
        }

        if (parameter.Schema is not null)
        {
            return parameter.Schema.MapOpenApiPrimitiveTypeToSimpleType();
        }

        throw new InvalidOperationException();
    }

    // Extended version of OpenApiTypeMapper.MapOpenApiPrimitiveTypeToSimpleType.
    // See https://github.com/microsoft/OpenAPI.NET/issues/1657.
    private static Type MapOpenApiPrimitiveTypeToSimpleType(this OpenApiSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var schemaAttributes = (
            schema.Type?.ToLowerInvariant(),
            schema.Format?.ToLowerInvariant(),
            schema.Nullable);

        var type = schemaAttributes switch
        {
            ("boolean", null, false) => typeof(bool),
            ("integer", null, false) => typeof(int),
            ("integer", "int32", false) => typeof(int),
            ("integer", "int64", false) => typeof(long),
            ("number", null, false) => typeof(double),
            ("number", "float", false) => typeof(float),
            ("number", "double", false) => typeof(double),
            ("number", "decimal", false) => typeof(decimal),
            ("string", "byte", false) => typeof(byte),
            ("string", "date-time", false) => typeof(DateTimeOffset),
            ("string", "uuid", false) => typeof(Guid),
            ("string", "duration", false) => typeof(TimeSpan),
            ("string", "char", false) => typeof(char),
            ("string", null, false) => typeof(string),
            ("object", null, false) => typeof(object),
            ("string", "uri", false) => typeof(Uri),
            ("integer", null, true) => typeof(int?),
            ("integer", "int32", true) => typeof(int?),
            ("integer", "int64", true) => typeof(long?),
            ("number", null, true) => typeof(double?),
            ("number", "float", true) => typeof(float?),
            ("number", "double", true) => typeof(double?),
            ("number", "decimal", true) => typeof(decimal?),
            ("string", "byte", true) => typeof(byte?),
            ("string", "date-time", true) => typeof(DateTimeOffset?),
            ("string", "uuid", true) => typeof(Guid?),
            ("string", "char", true) => typeof(char?),
            ("boolean", null, true) => typeof(bool?),
            _ => typeof(string),
        };

        return type;
    }
}
