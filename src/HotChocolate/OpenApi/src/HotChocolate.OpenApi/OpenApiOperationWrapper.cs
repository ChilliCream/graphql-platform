using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi;

internal sealed class OpenApiOperationWrapper(
    OpenApiOperation operation,
    OperationType operationType,
    string path)
{
    public readonly string OperationId = operation.OperationId ?? $"{operationType} {path}";

    public OpenApiOperation Operation { get; } = operation;

    public OperationType Type { get; } = operationType;

    public string Path { get; } = path;
}
