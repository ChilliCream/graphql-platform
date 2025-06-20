using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution;

public abstract class FieldResult : ResultData
{
    public Selection Selection { get; protected set; } = null!;

    protected internal virtual void Initialize(Selection selection)
    {
        Selection = selection;
    }

    public override void Reset()
    {
        Selection = null!;
    }
}
