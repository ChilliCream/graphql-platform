namespace HotChocolate.Types.Analyzers.Models;

public sealed class RequestMiddlewareParameterInfo(
    RequestMiddlewareParameterKind kind,
    string? typeName,
    bool isNullable = false)
    : IEquatable<RequestMiddlewareParameterInfo>
{
    public RequestMiddlewareParameterKind Kind { get; } = kind;

    public string? TypeName { get; } = typeName;

    public bool IsNullable { get; } = isNullable;

    public bool Equals(RequestMiddlewareParameterInfo? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Kind == other.Kind
            && TypeName == other.TypeName
            && IsNullable == other.IsNullable;
    }

    public override bool Equals(object? obj)
        => obj is RequestMiddlewareParameterInfo other
            && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Kind, TypeName, IsNullable);
}
