using HotChocolate.Stitching.Types.Attempt1.Wip;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

public class MergeOperationContext : OperationContextBase
{
    public IMergeOperationsProvider OperationProvider { get; }

    public MergeOperationContext(
        ISchemaDatabase schemaDatabase,
        IMergeOperationsProvider operationProvider)
        : base(schemaDatabase)
    {
        OperationProvider = operationProvider;
    }
}
