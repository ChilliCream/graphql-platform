using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal static class SchemaCoordinateExtensions
{
    public static SchemaCoordinate2 CreateChild(this SchemaCoordinate2 parent, ISyntaxNode node)
    {
        if (node is INamedSyntaxNode namedSyntaxNode)
        {
            return new SchemaCoordinate2(parent, namedSyntaxNode.Name);
        }

        return new SchemaCoordinate2(parent);
    }
}