using Microsoft.AspNetCore.OpenApi;
#if NET10_0_OR_GREATER
using Microsoft.OpenApi;
#else
using Microsoft.OpenApi.Models;
#endif

namespace HotChocolate.Exporters.OpenApi;

internal sealed class DynamicOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        // Add dynamic routes to the OpenAPI document
        // foreach (var endpoint in dataSource.Endpoints)
        // {
        //     if (endpoint is RouteEndpoint routeEndpoint)
        //     {
        //         var pattern = routeEndpoint.RoutePattern.RawText ?? "";
        //         var openApiPath = ConvertRoutePatternToOpenApiPath(pattern);
        //
        //         if (!document.Paths.ContainsKey(openApiPath))
        //         {
        //             document.Paths[openApiPath] = new OpenApiPathItem();
        //         }
        //
        //         // Get OpenAPI metadata if present
        //         var operation = endpoint.Metadata.GetMetadata<OpenApiOperation>()
        //             ?? new OpenApiOperation
        //             {
        //                 OperationId = routeEndpoint.DisplayName,
        //                 Summary = $"Dynamic endpoint: {pattern}",
        //                 Responses = new OpenApiResponses
        //                 {
        //                     ["200"] = new OpenApiResponse { Description = "Success" }
        //                 }
        //             };
        //
        //         // Get HTTP method
        //         var httpMethod = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.FirstOrDefault() ?? "GET";
        //
        //         // Add parameters from route pattern
        //         operation.Parameters ??= new List<OpenApiParameter>();
        //         foreach (var parameter in routeEndpoint.RoutePattern.Parameters)
        //         {
        //             // if (!operation.Parameters.Any(p => p.Name == parameter.Name))
        //             // {
        //             //     operation.Parameters.Add(new OpenApiParameter
        //             //     {
        //             //         Name = parameter.Name,
        //             //         In = ParameterLocation.Path,
        //             //         Required = !parameter.IsOptional,
        //             //         Schema = GetSchemaForParameter(parameter)
        //             //     });
        //             // }
        //         }
        //
        //         // Map HTTP method to OpenAPI operation type
        //         var operationType = httpMethod.ToUpperInvariant() switch
        //         {
        //             "GET" => OperationType.Get,
        //             "POST" => OperationType.Post,
        //             "PUT" => OperationType.Put,
        //             "DELETE" => OperationType.Delete,
        //             "PATCH" => OperationType.Patch,
        //             "OPTIONS" => OperationType.Options,
        //             "HEAD" => OperationType.Head,
        //             _ => OperationType.Get
        //         };
        //
        //         document.Paths[openApiPath].Operations[operationType] = operation;
        //     }
        // }

        return Task.CompletedTask;
    }

    // private string ConvertRoutePatternToOpenApiPath(string pattern)
    // {
    //     // Convert ASP.NET route pattern to OpenAPI path
    //     // {userId:int} -> {userId}
    //     // {*path} -> {path}
    //     return System.Text.RegularExpressions.Regex.Replace(
    //         pattern,
    //         @"\{([^}:]+)(?::[^}]+)?\}|\{\*([^}]+)\}",
    //         match => $"{{{match.Groups[1].Value}{match.Groups[2].Value}}}");
    // }

    // private OpenApiSchema GetSchemaForParameter(RoutePatternParameter parameter)
    // {
    //     // Try to infer type from constraint
    //     var constraint = parameter.ParameterPolicies.FirstOrDefault()?.Content?.ToLowerInvariant();
    //
    //     return constraint switch
    //     {
    //         "int" or "long" => new OpenApiSchema { Type = "integer" },
    //         "guid" => new OpenApiSchema { Type = "string", Format = "uuid" },
    //         "bool" => new OpenApiSchema { Type = "boolean" },
    //         "decimal" or "double" or "float" => new OpenApiSchema { Type = "number" },
    //         "datetime" => new OpenApiSchema { Type = "string", Format = "date-time" },
    //         _ => new OpenApiSchema { Type = "string" }
    //     };
    // }
}
