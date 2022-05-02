using HotChocolate.Stitching.Types.Attempt1.Wip;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

public class MergeOperationContext : OperationContextBase
{
    public IMergeOperationsProvider OperationsProvider { get; }

    public MergeOperationContext(IMergeOperationsProvider operationsProvider)
    {
        OperationsProvider = operationsProvider;
    }
}
