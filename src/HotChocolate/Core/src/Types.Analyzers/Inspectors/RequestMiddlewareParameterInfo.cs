namespace HotChocolate.Types.Analyzers.Inspectors;

public sealed class RequestMiddlewareParameterInfo(
    RequestMiddlewareParameterKind kind,
    string? typeName,
    bool isNullable = false)
{
    public RequestMiddlewareParameterKind Kind { get; } = kind;

    public string? TypeName { get; } = typeName;

    public bool IsNullable { get; } = isNullable;
}