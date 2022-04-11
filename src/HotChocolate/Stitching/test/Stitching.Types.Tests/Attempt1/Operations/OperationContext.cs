namespace HotChocolate.Stitching.Types;

internal class OperationContext
{
    public IOperationProvider OperationProvider { get; }

    public OperationContext(IOperationProvider operationProvider)
    {
        OperationProvider = operationProvider;
    }
}