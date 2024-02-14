namespace HotChocolate.Types.Analyzers.Inspectors;

public sealed class OperationRegistrationInfo(OperationType type, string typeName) : ISyntaxInfo
{
    public OperationType Type { get; } = type;
    
    public string TypeName { get; } = typeName;

    public bool Equals(OperationRegistrationInfo? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return TypeName.Equals(other.TypeName, StringComparison.Ordinal);
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
            hashCode = (hashCode * 397) ^ TypeName.GetHashCode();
            return hashCode;
        }
    }
}