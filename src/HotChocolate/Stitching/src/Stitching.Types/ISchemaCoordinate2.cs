using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

public interface ISchemaCoordinate2
{
    ISchemaCoordinate2? Parent { get; }
    SyntaxKind Kind { get; }
    NameNode? Name { get; }
}
