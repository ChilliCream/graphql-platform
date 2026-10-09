using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationInfo : RequestFeature
{
    public Operation? Operation { get; set; }

    public OperationDefinitionNode? Definition { get; set; }

    protected internal override void Reset()
    {
        Definition = null;
        Operation = null;
    }
}
