namespace HotChocolate.Types.Analyzers.Models;

public sealed class OperationInfo(OperationType type, string typeName, string methodName) : SyntaxInfo
{
    public OperationType Type { get; } = type;

    public string TypeName { get; } = typeName;

    public string MethodName { get; } = methodName;

    public override string OrderByKey => TypeName;

    public override bool Equals(object? obj)
        => obj is OperationInfo other && Equals(other);

    public override bool Equals(SyntaxInfo obj)
        => obj is OperationInfo info && Equals(info);

    private bool Equals(OperationInfo other)
        => Type.Equals(other.Type)
            && TypeName.Equals(other.TypeName, StringComparison.Ordinal)
            && MethodName.Equals(other.MethodName, StringComparison.Ordinal);

    public override int GetHashCode()
        => HashCode.Combine(Type, TypeName, MethodName);
}
