using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

public class MergeOperationContext : OperationContextBase
{
    public NameNode? Source { get; }
    public NameNode? Destination { get; }

    public MergeOperationContext(NameNode? source = default, NameNode? destination = default)
    {
        Source = source;
        Destination = destination;
    }
}
