using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Extensions;

internal static class ObjectTypeDefinitionExtensions
{
    public static bool IsInterfaceNode(this ObjectTypeDefinition objectTypeDefinition, ISchemaNode typeReferenceNode)
    {
        return IsInterfaceNode(objectTypeDefinition.Definition, typeReferenceNode.Definition);
    }

    public static bool IsInterfaceNode(this ObjectTypeDefinitionNode objectTypeDefinition, ISyntaxNode typeReferenceNode)
    {
        if (typeReferenceNode is not NamedTypeNode namedTypeNode)
        {
            return false;
        }

        return objectTypeDefinition.Interfaces
            .Contains(namedTypeNode);
    }
}
