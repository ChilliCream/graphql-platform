using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Extensions;

public static class InheritanceExtensions
{
    public static bool Inherits(this ObjectTypeDefinitionNode typeNode,
        InterfaceTypeDefinitionNode interfaceNode)
    {
        return typeNode.Interfaces
            .Any(x => x.Name.Equals(interfaceNode.Name));
    }
}
