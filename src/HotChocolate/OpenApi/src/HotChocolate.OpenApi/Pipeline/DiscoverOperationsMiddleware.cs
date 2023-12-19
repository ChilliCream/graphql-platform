using System.Text.RegularExpressions;
using HotChocolate.OpenApi.Helpers;
using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi.Pipeline;

/// <summary>
/// Discovers all operations of an open api document
/// </summary>
internal partial class DiscoverOperationsMiddleware : IOpenApiWrapperMiddleware
{
    private readonly Regex _successfulStatusCode = SuccessfulRegex();

    /// <inheritdoc />
    public void Invoke(OpenApiWrapperContext context, OpenApiWrapperDelegate next)
    {
        foreach (var openApiPath in context.OpenApiDocument.Paths)
        {
            var path = openApiPath.Value;
            foreach (var operationKeyValue in path.Operations)
            {
                var response = operationKeyValue.Value.Responses
                    .FirstOrDefault(r => _successfulStatusCode.IsMatch(r.Key));

                var resultOperation = new Operation(
                    operationKeyValue.Value.OperationId ?? 
                        OpenApiNamingHelper.GetPathAsName(openApiPath.Key),
                    openApiPath.Key,
                    new HttpMethod(operationKeyValue.Key.ToString()),
                    operationKeyValue.Value)
                {
                    Description = string.IsNullOrEmpty(operationKeyValue.Value.Description)
                        ? operationKeyValue.Value.Summary
                        : operationKeyValue.Value.Description,
                    Response = response.Value,
                    RequestBody = operationKeyValue.Value.RequestBody,
                };

                foreach (var openApiParameter in operationKeyValue.Value.Parameters)
                {
                    resultOperation.AddParameter(openApiParameter);
                }

                if (context.Operations.ContainsKey(resultOperation.OperationId))
                {
                    continue;
                }

                context.Operations.Add(resultOperation.OperationId, resultOperation);
            }
        }

        next.Invoke(context);
    }

    [GeneratedRegex("2[0-9]{2}|2XX", RegexOptions.Compiled)]
    private static partial Regex SuccessfulRegex();
}
