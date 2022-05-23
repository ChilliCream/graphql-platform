using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Extensions;

public static class DirectiveExtensions
{
    public static TSyntaxNode ReplaceDirective<TSyntaxNode>(
        this TSyntaxNode node,
        DirectiveNode find,
        DirectiveNode replace)
        where TSyntaxNode : class, ISyntaxNode
    {
        var directives = new List<DirectiveNode>(node.GetDirectives());

        var matchingDirective = directives
            .FindIndex(directive => SyntaxComparer.BySyntax.Equals(directive, find));

        if (matchingDirective >= 0)
        {
            directives[matchingDirective] = replace;
        }
        else
        {
            throw new KeyNotFoundException("Could not find the directive to replace");
        }

        node = UpdateDirectives(node, directives) as TSyntaxNode
               ?? node;

        return node;
    }

    private static ISyntaxNode UpdateDirectives(
        ISyntaxNode node,
        IReadOnlyList<DirectiveNode> directives)
    {
        return node switch
        {
            EnumTypeDefinitionNode enumTypeDefinitionNode
                => enumTypeDefinitionNode.WithDirectives(directives),
            InputObjectTypeDefinitionNode inputObjectTypeDefinitionNode
                => inputObjectTypeDefinitionNode.WithDirectives(directives),
            InterfaceTypeDefinitionNode interfaceTypeDefinitionNode
                => interfaceTypeDefinitionNode.WithDirectives(directives),
            ObjectTypeDefinitionNode objectTypeDefinitionNode
                => objectTypeDefinitionNode.WithDirectives(directives),
            ScalarTypeDefinitionNode scalarTypeDefinitionNode
                => scalarTypeDefinitionNode.WithDirectives(directives),
            UnionTypeDefinitionNode unionTypeDefinitionNode
                => unionTypeDefinitionNode.WithDirectives(directives),
            _ => throw new ArgumentOutOfRangeException(nameof(node))
        };
    }

    public static IReadOnlyList<DirectiveNode> GetDirectives(this ISyntaxNode node)
    {
        return node switch
        {
            EnumTypeDefinitionNode enumTypeDefinitionNode => enumTypeDefinitionNode.Directives,
            InputObjectTypeDefinitionNode inputObjectTypeDefinitionNode =>
                inputObjectTypeDefinitionNode.Directives,
            InterfaceTypeDefinitionNode interfaceTypeDefinitionNode => interfaceTypeDefinitionNode.Directives,
            ObjectTypeDefinitionNode objectTypeDefinitionNode => objectTypeDefinitionNode.Directives,
            ScalarTypeDefinitionNode scalarTypeDefinitionNode => scalarTypeDefinitionNode.Directives,
            UnionTypeDefinitionNode unionTypeDefinitionNode => unionTypeDefinitionNode.Directives,
            _ => throw new ArgumentOutOfRangeException(nameof(node))
        };
    }
}
