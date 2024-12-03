namespace HotChocolate.Types.Analyzers.Models;

public sealed class OperationRegistrationInfo(OperationType type, string typeName) : SyntaxInfo
{
    public OperationType Type { get; } = type;

    public string TypeName { get; } = typeName;

    public override string OrderByKey => TypeName;

    public override bool Equals(object? obj)
        => obj is OperationRegistrationInfo other && Equals(other);

    public override bool Equals(SyntaxInfo other)
        => other is OperationRegistrationInfo info && Equals(info);

    private bool Equals(OperationRegistrationInfo other)
        => Type.Equals(other.Type)
            && TypeName.Equals(other.TypeName, StringComparison.Ordinal);

    public override int GetHashCode()
        => HashCode.Combine(Type, TypeName);
}
