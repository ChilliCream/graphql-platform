using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using static HotChocolate.Language.SyntaxKind;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyRenaming;

internal sealed class RenameRewriter : SyntaxRewriter<RenameContext>
{
    protected override RenameContext OnEnter(ISyntaxNode node, RenameContext context)
    {
        context.Navigator.Push(node);
        return base.OnEnter(node, context);
    }

    protected override void OnLeave(ISyntaxNode node, RenameContext context)
    {
        context.Navigator.Pop();
        base.OnLeave(node, context);
    }

    protected override ObjectTypeDefinitionNode? RewriteObjectTypeDefinition(
        ObjectTypeDefinitionNode node,
        RenameContext context)
    {
        var originalName = node.Name.Value;

        var rewrittenNode = base.RewriteObjectTypeDefinition(node, context);
        if (rewrittenNode is null)
        {
            return default;
        }

        if (context.RenamedTypes.TryGetValue(originalName, out var _))
        {
            rewrittenNode = ApplyBindDirective(
                rewrittenNode,
                context,
                originalName,
                static (n, d) => n.WithDirectives(d));
        }

        return rewrittenNode;
    }

    protected override InterfaceTypeDefinitionNode? RewriteInterfaceTypeDefinition(
        InterfaceTypeDefinitionNode node,
        RenameContext context)
    {
        var originalName = node.Name.Value;

        var rewrittenNode = base.RewriteInterfaceTypeDefinition(node, context);
        if (rewrittenNode is null)
        {
            return default;
        }

        if (context.RenamedTypes.TryGetValue(originalName, out var _))
        {
            rewrittenNode = ApplyBindDirective(
                rewrittenNode,
                context,
                originalName,
                static (n, d) => n.WithDirectives(d));
        }

        return rewrittenNode;
    }

    protected override UnionTypeDefinitionNode? RewriteUnionTypeDefinition(
        UnionTypeDefinitionNode node,
        RenameContext context)
    {
        var originalName = node.Name.Value;

        var rewrittenNode = base.RewriteUnionTypeDefinition(node, context);
        if (rewrittenNode is null)
        {
            return default;
        }

        if (context.RenamedTypes.TryGetValue(originalName, out var _))
        {
            rewrittenNode = ApplyBindDirective(
                rewrittenNode,
                context,
                originalName,
                static (n, d) => n.WithDirectives(d));
        }

        return rewrittenNode;
    }

    protected override InputObjectTypeDefinitionNode? RewriteInputObjectTypeDefinition(
        InputObjectTypeDefinitionNode node,
        RenameContext context)
    {
        var originalName = node.Name.Value;

        var rewrittenNode = base.RewriteInputObjectTypeDefinition(node, context);
        if (rewrittenNode is null)
        {
            return default;
        }

        if (context.RenamedTypes.TryGetValue(originalName, out var _))
        {
            rewrittenNode = ApplyBindDirective(
                rewrittenNode,
                context,
                originalName,
                static (n, d) => n.WithDirectives(d));
        }

        return rewrittenNode;
    }

    protected override EnumTypeDefinitionNode? RewriteEnumTypeDefinition(
        EnumTypeDefinitionNode node,
        RenameContext context)
    {
        var originalName = node.Name.Value;

        var rewrittenNode = base.RewriteEnumTypeDefinition(node, context);
        if (rewrittenNode is null)
        {
            return default;
        }

        if (context.RenamedTypes.TryGetValue(originalName, out var _))
        {
            rewrittenNode = ApplyBindDirective(
                rewrittenNode,
                context,
                originalName,
                static (n, d) => n.WithDirectives(d));
        }

        return rewrittenNode;
    }

    protected override ScalarTypeDefinitionNode? RewriteScalarTypeDefinition(
        ScalarTypeDefinitionNode node,
        RenameContext context)
    {
        var originalName = node.Name.Value;

        var rewrittenNode = base.RewriteScalarTypeDefinition(node, context);
        if (rewrittenNode is null)
        {
            return default;
        }

        if (context.RenamedTypes.TryGetValue(originalName, out var _))
        {
            rewrittenNode = ApplyBindDirective(
                rewrittenNode,
                context,
                originalName,
                static (n, d) => n.WithDirectives(d));
        }

        return rewrittenNode;
    }

    protected override FieldDefinitionNode? RewriteFieldDefinition(
        FieldDefinitionNode node,
        RenameContext context)
    {
        var rewrittenNode = base.RewriteFieldDefinition(node, context);
        if (rewrittenNode is null)
        {
            return default;
        }

        if (context.Navigator.TryPeek(1, out var parent) &&
            parent.Kind is SyntaxKind.ObjectTypeDefinition or InterfaceTypeDefinition &&
            context.TypesWithFieldRenames.Contains(((INamedSyntaxNode)parent).Name.Value))
        {
            var coordinate = context.Navigator.CreateCoordinate();
            if (context.RenamedFields.TryGetValue(coordinate, out var value))
            {
                var location = rewrittenNode.Location;
                var name = new NameNode(value.Name);
                var description = rewrittenNode.Description;
                var arguments = rewrittenNode.Arguments;
                var type = rewrittenNode.Type;
                var directives = rewrittenNode.Directives.ToList();
                directives.Remove(value.RenameDirective);
                directives.Add(new BindDirective(context.SourceName, rewrittenNode.Name.Value));

                rewrittenNode = new FieldDefinitionNode(
                    location,
                    name,
                    description,
                    arguments,
                    type,
                    directives);
            }
        }

        return rewrittenNode;
    }

    protected override NameNode RewriteName(NameNode node, RenameContext context)
    {
        if (!context.Navigator.TryPeek(1, out var parent))
        {
            return base.RewriteName(node, context);
        }

        if (parent.Kind is SyntaxKind.ObjectTypeDefinition or
                InterfaceTypeDefinition or
                UnionTypeDefinition or
                InputObjectTypeDefinition or
                EnumTypeDefinition or
                ScalarTypeDefinition or
                NamedType &&
            context.RenamedTypes.TryGetValue(node.Value, out var value))
        {
            return node.WithValue(value.Name);
        }

        if (!context.Navigator.TryPeek(2, out var grandParent))
        {
            return base.RewriteName(node, context);
        }

        // rename interface implements
        if (grandParent.Kind is SyntaxKind.ObjectTypeDefinition or InterfaceTypeDefinition  &&
            parent.Kind is NamedType &&
            context.RenamedTypes.TryGetValue(node.Value, out value))
        {
            return node.WithValue(value.Name);
        }

        return base.RewriteName(node, context);
    }

    private static T ApplyBindDirective<T>(
        T node,
        RenameContext context,
        string originalName,
        Func<T, IReadOnlyList<DirectiveNode>, T> factory)
        where T : IHasDirectives
    {
        if (node.Directives.Count == 1)
        {
            return factory(
                node,
                new DirectiveNode[]
                {
                    new BindDirective(context.SourceName, originalName)
                });
        }

        var copy = node.Directives.ToList();

        foreach (var directive in node.Directives)
        {
            if (RenameDirective.IsOfType(directive))
            {
                copy.Remove(directive);
            }
        }

        copy.Add(new BindDirective(context.SourceName, originalName));
        return factory(node, copy);
    }
}
