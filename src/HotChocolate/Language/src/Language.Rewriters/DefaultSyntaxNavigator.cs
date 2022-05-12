using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language.Rewriters.Contracts;
using HotChocolate.Language.Rewriters.Utilities;

namespace HotChocolate.Language.Rewriters;

public class DefaultSyntaxNavigator : ISyntaxNavigator
{
    private readonly Stack<ISyntaxNode> _stack = new();

    public TNode? GetAncestor<TNode>()
        where TNode : ISyntaxNode
    {
        return _stack.OfType<TNode>().LastOrDefault();
    }

    public IEnumerable<TNode> GetAncestors<TNode>()
        where TNode : ISyntaxNode
    {
        return _stack.OfType<TNode>();
    }

    public IDisposable Push(ISyntaxNode node)
    {
        _stack.Push(node);
        return new DisposableAction(Pop);
    }

    private void Pop()
    {
        _stack.Pop();
    }

    public SchemaCoordinate CreateCoordinate()
    {
        var namedSyntaxNodes = GetCoordinateNames()
            .ToList();

        switch (namedSyntaxNodes.Count)
        {
            case 1:
                return new SchemaCoordinate(new NameString(namedSyntaxNodes[0].Value));

            case 2:
                return new SchemaCoordinate(
                    new NameString(namedSyntaxNodes[1].Value),
                    new NameString(namedSyntaxNodes[0].Value));
            case 3:
                return new SchemaCoordinate(
                    new NameString(namedSyntaxNodes[2].Value),
                    new NameString(namedSyntaxNodes[1].Value),
                    new NameString(namedSyntaxNodes[0].Value));
        }

        return new SchemaCoordinate();
    }

    private IEnumerable<NameNode> GetCoordinateNames()
    {
        foreach (INamedSyntaxNode namedSyntaxNode in GetAncestors<INamedSyntaxNode>())
        {
            NameNode defaultName = namedSyntaxNode.Name;
            if (!namedSyntaxNode.TryGetSource(out SourceDirective? sourceDirective))
            {
                yield return defaultName;
                continue;
            }

            SchemaCoordinateNode schemaCoordinateNode = sourceDirective.Coordinate
                .ToSyntax();

            foreach (NameNode node in schemaCoordinateNode.GetNodes().OfType<NameNode>())
            {
                yield return node;
            }

            yield break;
        }
    }
}
