using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyRenaming;

public sealed class RenameInfo
{
    public RenameInfo(string name, DirectiveNode renameDirective)
    {
        Name = name;
        RenameDirective = renameDirective;
    }

    public string Name { get; }

    public DirectiveNode RenameDirective { get; }
}
