using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal interface ISchemaCoordinate2Provider
{
    ISchemaNode Root { get; }
    ISchemaCoordinate2 Add(ISchemaNode node);
    ISchemaNode? Get(ISchemaCoordinate2? coordinate);
    bool TryGet(ISchemaCoordinate2? coordinate, [NotNullWhen(true)] out ISchemaNode? schemaNode);
    ISchemaCoordinate2? Get(ISchemaNode node);

    void Associate<TDefinition>(ISchemaCoordinate2 coordinate, TDefinition typedDestination)
        where TDefinition : ISchemaNode;

    ISchemaCoordinate2? Get(ISyntaxNode node);
    bool TryGet(ISchemaNode? node, [NotNullWhen(true)] out ISchemaCoordinate2? coordinate);
    bool TryGet(ISyntaxNode? node, [NotNullWhen(true)] out ISchemaCoordinate2? coordinate);
    ISchemaCoordinate2 CalculateCoordinate(ISchemaNode node);
    ISchemaCoordinate2 CalculateCoordinate(ISchemaNode? parent, ISyntaxNode node);
}

internal interface ISchemaCoordinate2
{
    ISchemaCoordinate2Provider Provider { get; }
    ISchemaCoordinate2? Parent { get; }
    SyntaxKind Kind { get; }
    NameNode? Name { get; }
    bool IsMatch(ISchemaCoordinate2 other);
}

internal interface ISchemaNode
{
    ISyntaxNode Definition { get; }
    ISchemaNode? Parent { get; }
    ISchemaCoordinate2 Coordinate { get; }
    void RewriteDefinition(ISyntaxNode node);
}

internal interface ISchemaNodeInfo<out TDefinition>
    where TDefinition : ISyntaxNode
{
    /// <summary>
    /// Gets the type definition syntax.
    /// </summary>
    TDefinition Definition { get; }
}

internal interface ISchemaNode<TDefinition> : ISchemaNodeInfo<TDefinition>, ISchemaNode
    where TDefinition : ISyntaxNode
{
    ISyntaxNode ISchemaNode.Definition
    {
        get => Definition;
    }

    void ISchemaNode.RewriteDefinition(ISyntaxNode node)
    {
        RewriteDefinition((TDefinition)node);
    }

    void RewriteDefinition(TDefinition node);

    /// <summary>
    /// Gets the type definition syntax.
    /// </summary>
    new TDefinition Definition { get; }
}
