using System.Linq;

namespace HotChocolate.Language.Rewriters.Extensions;

public static class InheritanceExtensions
{
    public static bool Inherits(this ObjectTypeDefinitionNode typeNode,
        InterfaceTypeDefinitionNode interfaceNode)
    {
        return typeNode.Interfaces
            .Any(x => x.Name.Equals(interfaceNode.Name));
    }
}
