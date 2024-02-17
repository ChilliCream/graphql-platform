namespace HotChocolate.Types.Analyzers.Inspectors;

public sealed class RequestMiddlewareParameterInfo(
    RequestMiddlewareParameterKind kind,
    string? typeName,
    bool isNullable = false) : IEquatable<RequestMiddlewareParameterInfo>
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
        
        return Kind == other.Kind && TypeName == other.TypeName && IsNullable == other.IsNullable;
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || obj is RequestMiddlewareParameterInfo other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (int)Kind;
            hashCode = (hashCode * 397) ^
                (TypeName != null
                    ? TypeName.GetHashCode()
                    : 0);
            hashCode = (hashCode * 397) ^ IsNullable.GetHashCode();
            return hashCode;
        }
    }
}