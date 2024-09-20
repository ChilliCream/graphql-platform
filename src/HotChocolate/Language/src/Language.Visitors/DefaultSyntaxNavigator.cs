using System.Diagnostics.CodeAnalysis;
using static HotChocolate.Language.Properties.Resources;

namespace HotChocolate.Language.Visitors;

/// <summary>
/// Represents the default implementation of <see cref="ISyntaxNavigator" />
/// </summary>
public class DefaultSyntaxNavigator : ISyntaxNavigator
{
    private readonly List<ISyntaxNode> _ancestors = [];
    private readonly ISyntaxNode[] _coordinate = new ISyntaxNode[3];

    /// <inheritdoc cref="ISyntaxNavigator.Count"/>
    public int Count => _ancestors.Count;

    /// <inheritdoc cref="ISyntaxNavigator.Push"/>
    public void Push(ISyntaxNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        _ancestors.Add(node);
    }

    /// <inheritdoc cref="ISyntaxNavigator.Pop"/>
    public ISyntaxNode Pop()
    {
        if (!TryPop(out var node))
        {
            throw new InvalidOperationException(DefaultSyntaxNavigator_Pop_StackEmpty);
        }

        return node;
    }

    /// <inheritdoc cref="ISyntaxNavigator.Peek()"/>
    public ISyntaxNode Peek()
    {
        if (!TryPeek(out var node))
        {
            throw new InvalidOperationException(DefaultSyntaxNavigator_Pop_StackEmpty);
        }

        return node;
    }

    /// <inheritdoc cref="ISyntaxNavigator.Peek()"/>
    public ISyntaxNode Peek(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                count,
                DefaultSyntaxNavigator_Peek_CountCannotBeNegative);
        }

        var depth = count + 1;

        if (_ancestors.Count < depth)
        {
            throw new InvalidOperationException(DefaultSyntaxNavigator_Pop_StackEmpty);
        }

        depth = _ancestors.Count - depth;
        return _ancestors[depth];
    }

    /// <inheritdoc cref="ISyntaxNavigator.TryPop"/>
    public bool TryPop([NotNullWhen(true)] out ISyntaxNode? node)
    {
        if (_ancestors.Count == 0)
        {
            node = default;
            return false;
        }

        node = _ancestors[_ancestors.Count - 1];
        _ancestors.RemoveAt(_ancestors.Count - 1);
        return true;
    }

    /// <inheritdoc cref="ISyntaxNavigator.TryPeek(out HotChocolate.Language.ISyntaxNode?)"/>
    public bool TryPeek([NotNullWhen(true)] out ISyntaxNode? node)
    {
        if (_ancestors.Count == 0)
        {
            node = default;
            return false;
        }

        node = _ancestors[_ancestors.Count - 1];
        return true;
    }

    /// <inheritdoc cref="ISyntaxNavigator.TryPeek(int,out HotChocolate.Language.ISyntaxNode?)"/>
    public bool TryPeek(int count, [NotNullWhen(true)] out ISyntaxNode? node)
    {
        if (_ancestors.Count < count)
        {
            node = default;
            return false;
        }

        node = _ancestors[_ancestors.Count - 1 - count];
        return true;
    }

    /// <inheritdoc cref="ISyntaxNavigator.GetAncestor{TNode}"/>
    public TNode? GetAncestor<TNode>()
        where TNode : ISyntaxNode
    {
        for (var i = _ancestors.Count - 1; i >= 0; i--)
        {
            if (_ancestors[i] is TNode typedNode)
            {
                return typedNode;
            }
        }

        return default;
    }

    /// <inheritdoc cref="ISyntaxNavigator.GetAncestors{TNode}"/>
    public IEnumerable<TNode> GetAncestors<TNode>()
        where TNode : ISyntaxNode
        => _ancestors.Count == 0 ? [] : GetAncestorsInternal<TNode>();

    private IEnumerable<TNode> GetAncestorsInternal<TNode>()
        where TNode : ISyntaxNode
    {
        for (var i = _ancestors.Count - 1; i >= 0; i--)
        {
            if (_ancestors[i] is TNode typedNode)
            {
                yield return typedNode;
            }
        }
    }

    /// <inheritdoc cref="ISyntaxNavigator.CreateCoordinate()"/>
    public SchemaCoordinateNode CreateCoordinate()
    {
        if (_ancestors.Count == 0)
        {
            throw new InvalidOperationException(
                DefaultSyntaxNavigator_CreateCoordinate_EmptyPath);
        }

        var p = 0;

        for (var i = _ancestors.Count - 1; i >= 0; i--)
        {
            var node = _ancestors[i];

            if (node.Kind is SyntaxKind.ScalarTypeDefinition
                or SyntaxKind.EnumTypeDefinition
                or SyntaxKind.InputObjectTypeDefinition
                or SyntaxKind.ObjectTypeDefinition
                or SyntaxKind.InterfaceTypeDefinition
                or SyntaxKind.UnionTypeDefinition
                or SyntaxKind.ScalarTypeExtension
                or SyntaxKind.EnumTypeExtension
                or SyntaxKind.InputObjectTypeExtension
                or SyntaxKind.ObjectTypeExtension
                or SyntaxKind.InterfaceTypeExtension
                or SyntaxKind.UnionTypeExtension
                or SyntaxKind.DirectiveDefinition
                or SyntaxKind.FieldDefinition
                or SyntaxKind.InputValueDefinition)
            {
                _coordinate[p++] = node;
            }

            if (p == 3)
            {
                break;
            }
        }

        if (p == 0)
        {
            throw new InvalidOperationException(
                DefaultSyntaxNavigator_CreateCoordinate_InvalidStructure);
        }

        var directive = false;
        NameNode? type;
        NameNode? field = null;
        NameNode? arg = null;

        var next = _coordinate[--p];

        if (next is DirectiveDefinitionNode directiveDefinition)
        {
            directive = true;
            type = directiveDefinition.Name;
        }
        else if (next is ITypeSystemDefinitionNode or ITypeSystemExtensionNode &&
            next is NamedSyntaxNode n)
        {
            type = n.Name;
        }
        else
        {
            throw new InvalidOperationException(
                DefaultSyntaxNavigator_CreateCoordinate_InvalidStructure);
        }

        while (p > 0)
        {
            next = _coordinate[--p];

            if (next is NamedSyntaxNode n)
            {
                if (!directive && field is null)
                {
                    field = n.Name;
                }
                else
                {
                    arg = n.Name;
                }
            }
        }

        _coordinate.AsSpan().Clear();
        return new SchemaCoordinateNode(null, directive, type, field, arg);
    }
}
