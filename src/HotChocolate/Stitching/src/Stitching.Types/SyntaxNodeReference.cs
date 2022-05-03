using System;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Traversal;

namespace HotChocolate.Stitching.Types;

public readonly struct SyntaxNodeReference : ISyntaxNodeReference
{
    public SyntaxNodeReference(ISyntaxNodeReference? parent, ISchemaCoordinate2 coordinate, ISyntaxNode node)
    {
        Parent = parent;
        Coordinate = coordinate ?? throw new ArgumentNullException(nameof(coordinate));
        Node = node ?? throw new ArgumentNullException(nameof(node));
    }

    public ISyntaxNodeReference? Parent { get; }
    public ISchemaCoordinate2 Coordinate { get; }

    public ISyntaxNode Node { get; }
}
