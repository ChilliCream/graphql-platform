using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt2;

public class RenameDirective
{
    private readonly DirectiveNode _directiveNode;

    public RenameDirective(DirectiveNode directiveNode)
    {
        _directiveNode = directiveNode;
    }

    public StringValueNode NewName =>
        _directiveNode.Arguments.FirstOrDefault(x
            => x.Name.Equals(new NameNode("name")))?.Value as StringValueNode
        ?? throw new InvalidFormatException();
}