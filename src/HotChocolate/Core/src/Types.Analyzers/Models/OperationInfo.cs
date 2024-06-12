namespace HotChocolate.Types.Analyzers.Models;

public sealed class OperationInfo(OperationType type, string typeName, string methodName) : ISyntaxInfo
{
    public OperationType Type { get; } = type;

    public string TypeName { get; } = typeName;

    public string MethodName { get; } = methodName;

    public bool Equals(OperationInfo? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Type.Equals(other.Type) && 
            TypeName.Equals(other.TypeName, StringComparison.Ordinal) &&
            MethodName.Equals(other.MethodName, StringComparison.Ordinal);
    }
    
    public bool Equals(ISyntaxInfo other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is OperationInfo info && Equals(info);
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj)
            || obj is DataLoaderInfo other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = 5;
            hashCode = (hashCode * 397) ^ Type.GetHashCode();
            hashCode = (hashCode * 397) ^ TypeName.GetHashCode();
            hashCode = (hashCode * 397) ^ MethodName.GetHashCode();
            return hashCode;
        }
    }
}