using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationInfo : RequestFeature
{
    public string? Id { get; set; }

    public IOperation? Operation { get; set; }

    public OperationDefinitionNode? Definition { get; set; }

    protected internal override void Reset()
    {
        Id = null;
        Definition = null;
        Operation = null;
    }
}
