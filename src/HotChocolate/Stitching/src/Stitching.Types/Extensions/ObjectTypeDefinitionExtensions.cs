using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Extensions;

internal static class ObjectTypeDefinitionExtensions
{
    public static bool IsMemberOfInterface(this ObjectTypeDefinition objectTypeDefinition, ISchemaNode typeReferenceNode)
    {
        return IsMemberOfInterface(objectTypeDefinition.Definition, typeReferenceNode.Definition);
    }

    public static bool IsMemberOfInterface(this ObjectTypeDefinitionNode objectTypeDefinition, ISyntaxNode typeReferenceNode)
    {
        if (typeReferenceNode is not NamedTypeNode namedTypeNode)
        {
            return false;
        }

        return objectTypeDefinition.Interfaces
            .Contains(namedTypeNode);
    }
}
