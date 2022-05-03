using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

public interface ISchemaDatabase
{
    ISchemaNode Reindex(ISchemaNode schemaNode);
    ISchemaNode Reindex(ISyntaxNode? parent, ISyntaxNode node);

    ISchemaNode GetOrAdd(SyntaxNodeReference nodeReference);
    ISchemaNode GetOrAdd(ISchemaCoordinate2 coordinate, ISyntaxNode node);
    ISchemaNode GetOrAdd(ISchemaNode parent, ISyntaxNode node);


    bool TryGet(ISyntaxNode? parent,
        ISyntaxNode node,
        [MaybeNullWhen(false)] out ISchemaNode existingNode);

    bool TryGet(
        ISchemaNode? parent,
        ISyntaxNode node,
        [MaybeNullWhen(false)] out ISchemaNode schemaNode);

    ISchemaCoordinate2 CalculateCoordinate(ISchemaCoordinate2? parentCoordinate, ISyntaxNode node);
    NameNode? Name { get; }
}
