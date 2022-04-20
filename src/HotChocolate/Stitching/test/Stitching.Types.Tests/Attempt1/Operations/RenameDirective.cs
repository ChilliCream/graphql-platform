using System;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal sealed class RenameDirective
{
    private static readonly NameNode _renameNode = new("rename");

    public DirectiveNode Node { get; }
    public NameNode NewName { get; }

    public RenameDirective(DirectiveNode node)
    {
        Node = node;
        NewName = CalculateNewName(node);
    }

    private static NameNode CalculateNewName(DirectiveNode node)
    {
        var nameArgument = node.Arguments
            .FirstOrDefault(x => x.Name.Value.Equals("name"))?
            .Value
            .Value;

        if (nameArgument is not string {Length: > 0} stringArgument)
        {
            throw new InvalidOperationException();
        }

        return new NameNode(stringArgument);
    }

    public static bool CanHandle(DirectiveNode directiveNode)
    {
        return directiveNode.Name
            .IsEqualTo(_renameNode);
    }
}
