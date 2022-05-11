using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt2;

public class SyntaxNavigator
{
    private readonly Stack<object> _stack = new();

    public IEnumerable<TNode> GetAncestors<TNode>()
        where TNode : ISyntaxNode
    {
        return _stack.OfType<TNode>();
    }

    public IDisposable Push(object node)
    {
        _stack.Push(node);
        return new DisposableAction(() =>
        {
            _stack.Pop();
        });
    }

    public SchemaCoordinate CreateCoordinate()
    {
        IReadOnlyList<INamedSyntaxNode> namedSyntaxNodes = GetAncestors<INamedSyntaxNode>()
            .Reverse()
            .ToList();

        switch (namedSyntaxNodes.Count)
        {
            case 1:
                return new SchemaCoordinate(new NameString(namedSyntaxNodes[0].Name.Value));
            case 2:
                return new SchemaCoordinate(
                    new NameString(namedSyntaxNodes[0].Name.Value),
                    new NameString(namedSyntaxNodes[1].Name.Value));
            case 3:
                return new SchemaCoordinate(
                    new NameString(namedSyntaxNodes[0].Name.Value),
                    new NameString(namedSyntaxNodes[1].Name.Value),
                    new NameString(namedSyntaxNodes[2].Name.Value));
        }

        return new SchemaCoordinate();
    }
}
