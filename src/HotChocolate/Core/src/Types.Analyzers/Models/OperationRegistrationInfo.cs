namespace HotChocolate.Types.Analyzers.Models;

public sealed class OperationRegistrationInfo(OperationType type, string typeName) : ISyntaxInfo
{
    public OperationType Type { get; } = type;

    public string TypeName { get; } = typeName;

    public override bool Equals(object? obj)
        => obj is OperationRegistrationInfo other && Equals(other);

    public bool Equals(ISyntaxInfo other)
        => other is OperationRegistrationInfo info && Equals(info);

    private bool Equals(OperationRegistrationInfo? other)
        => Type.Equals(Type)
            && TypeName.Equals(other.TypeName, StringComparison.Ordinal);

    public override int GetHashCode()
        => HashCode.Combine(Type, TypeName);
}
