using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

public class ListResult : ResultData
{
    public IType Type { get; private set; } = null!;

    public IType ElementType { get; private set; } = null!;

    public void Initialize(IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        Type = type;
        ElementType = type.ElementType();
    }

    public override void Reset()
    {
        Type = null!;
        ElementType = null!;

        base.Reset();
    }
}
