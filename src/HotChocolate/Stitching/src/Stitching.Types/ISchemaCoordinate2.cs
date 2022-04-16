using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal interface ISchemaCoordinate2
{
    ISchemaDatabase Database { get; }
    ISchemaCoordinate2? Parent { get; }
    SyntaxKind Kind { get; }
    NameNode? Name { get; }
    bool IsMatch(ISchemaCoordinate2 other);
}