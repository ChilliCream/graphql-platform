using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language.Rewriters.Contracts;
using HotChocolate.Language.Rewriters.Utilities;

namespace HotChocolate.Language.Rewriters;

public class DefaultSyntaxNavigator : ISyntaxNavigator
{
    private readonly List<ISyntaxNode> _ancestors = new();

    public TNode? GetAncestor<TNode>()
        where TNode : ISyntaxNode
    {
        for (var i = _ancestors.Count - 1; i >= 0; i--)
        {
            if (_ancestors[i] is not TNode typedNode)
            {
                continue;
            }

            return typedNode;
        }

        return default;
    }

    public IEnumerable<TNode> GetAncestors<TNode>()
        where TNode : ISyntaxNode
    {
        for (var i = _ancestors.Count - 1; i >= 0; i--)
        {
            if (_ancestors[i] is not TNode typedNode)
            {
                continue;
            }

            yield return typedNode;
        }
    }

    public IDisposable Push(ISyntaxNode node)
    {
        _ancestors.Add(node);
        return new DisposableAction(Pop);
    }

    private void Pop()
    {
        _ancestors.RemoveAt(_ancestors.Count - 1);
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
        IList<INamedSyntaxNode> namedSyntaxNodes = GetAncestors<INamedSyntaxNode>()
            .ToList();

        foreach (INamedSyntaxNode namedSyntaxNode in namedSyntaxNodes)
        {
            NameNode defaultName = namedSyntaxNode.Name;
            if (!namedSyntaxNode.TryGetSource(out SourceDirective? sourceDirective))
            {
                yield return defaultName;
                continue;
            }

            SchemaCoordinateNode schemaCoordinateNode = sourceDirective.Coordinate
                .ToSyntax();

            IEnumerable<ISyntaxNode> syntaxNodes = schemaCoordinateNode.GetNodes();
            foreach (ISyntaxNode syntaxNode in syntaxNodes)
            {
                if (syntaxNode is not NameNode nameNode)
                {
                    continue;
                }

                yield return nameNode;
            }
        }
    }
}
