using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal interface ISchemaDatabase
{
    ISchemaNode Root { get; }
    ISchemaCoordinate2 Add(ISchemaNode node);
    ISchemaNode? Get(ISchemaCoordinate2? coordinate);
    ISchemaCoordinate2? Get(ISchemaNode node);

    ISchemaNode Reindex(ISchemaNode schemaNode);
    ISchemaNode Reindex(ISyntaxNode? parent, ISyntaxNode node);

    ISchemaCoordinate2? Get(ISyntaxNode node);
    bool TryGet(ISchemaNode? node, [NotNullWhen(true)] out ISchemaCoordinate2? coordinate);
    bool TryGetExact(ISyntaxNode? node, [NotNullWhen(true)] out ISchemaCoordinate2? coordinate);
    bool TryGet(ISchemaNode? parent, ISyntaxNode node, [NotNullWhen(true)] out ISchemaNode? schemaNode);
    bool TryGetExact(ISyntaxNode? node, [NotNullWhen(true)] out ISchemaNode? schemaNode);
    bool TryGet(ISchemaCoordinate2? coordinate, [NotNullWhen(true)] out ISchemaNode? schemaNode);
    bool TryGet(ISyntaxNode? parent, ISyntaxNode node, [NotNullWhen(true)] out ISchemaNode? existingNode);
}
