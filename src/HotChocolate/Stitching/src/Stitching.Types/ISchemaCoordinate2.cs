using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

public interface ISchemaCoordinate2
{
    ISchemaCoordinate2? Parent { get; }
    NameNode? Name { get; }
    SyntaxKind Kind { get; }
    bool IsRoot { get; }
}
