using System.Text.RegularExpressions;
using HotChocolate.OpenApi.Models;
using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi.Pipeline;

internal class OperationDiscoveryMiddleware : IOpenApiWrapperMiddleware
{
    private readonly Regex _succesfulStatusCode = new("2[0-9]{2}|2XX", RegexOptions.Compiled);

    /// <inheritdoc />
    public void Invoke(OpenApiWrapperContext context, OpenApiWrapperDelegate next)
    {
        foreach (var openApiPath in context.OpenApiDocument.Paths)
        {
            var path = openApiPath.Value;
            foreach (var operationKeyValue in path.Operations.Select(o => o))
            {
                var response = operationKeyValue.Value.Responses
                    .FirstOrDefault(r => _succesfulStatusCode.IsMatch(r.Key));

                var resultOperation = new Operation(
                    operationKeyValue.Value.OperationId,
                    openApiPath.Key,
                    new HttpMethod(operationKeyValue.Key.ToString()),
                    operationKeyValue.Value)
                {
                    Description = string.IsNullOrEmpty(operationKeyValue.Value.Description)
                        ? operationKeyValue.Value.Summary
                        : operationKeyValue.Value.Description,
                    Response = response.Value,
                    Arguments = GetArguments(operationKeyValue.Value).ToList()
                };

                if (context.Operations.ContainsKey(resultOperation.OperationId)) continue;
                context.Operations.Add(resultOperation.OperationId, resultOperation);
            }
        }

        next.Invoke(context);
    }

    private static IEnumerable<Argument> GetArguments(OpenApiOperation openApiOperation)
    {
        if (openApiOperation.RequestBody is not null)
        {
            yield return new Argument
            {
                RequestBody = openApiOperation.RequestBody
            };
        }

        if (openApiOperation.Parameters is null) yield break;
        foreach (var parameter in openApiOperation.Parameters)
        {
            yield return new Argument
            {
                Parameter = parameter
            };
        }
    }
}
