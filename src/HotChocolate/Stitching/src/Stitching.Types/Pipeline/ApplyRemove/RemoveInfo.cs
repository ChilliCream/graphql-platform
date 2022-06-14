using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyRemove;

public sealed class RemoveInfo
{
    public RemoveInfo(DirectiveNode renameDirective)
    {
        RenameDirective = renameDirective;
    }

    public DirectiveNode RenameDirective { get; }
}
