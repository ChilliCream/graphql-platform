namespace HotChocolate.Stitching.Types;

public class OperationContext
{
    public IOperationProvider OperationProvider { get; }

    public OperationContext(IOperationProvider operationProvider)
    {
        OperationProvider = operationProvider;
    }
}
