using HotChocolate.Stitching.Types.Attempt1.Wip;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

public class OperationContext
{
    public IOperationProvider OperationProvider { get; }

    public OperationContext(IOperationProvider operationProvider)
    {
        OperationProvider = operationProvider;
    }
}
