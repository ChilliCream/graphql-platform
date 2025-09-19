namespace StrawberryShake;

public class OperationUpdate
{
    public OperationUpdate(OperationUpdateKind kind, IReadOnlyList<StoredOperationVersion> operationVersions)
    {
        Kind = kind;
        OperationVersions = operationVersions;
    }

    public OperationUpdateKind Kind { get; }

    public IReadOnlyList<StoredOperationVersion> OperationVersions { get; }
}
