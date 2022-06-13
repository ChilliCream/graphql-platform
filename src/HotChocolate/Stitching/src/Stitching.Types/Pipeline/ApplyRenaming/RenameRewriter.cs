using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyRenaming;

internal sealed class RenameRewriter : SyntaxRewriter<RewriteContext>
{
    protected override RewriteContext OnEnter(ISyntaxNode node, RewriteContext context)
    {
        context.Navigator.Push(node);
        return base.OnEnter(node, context);
    }

    protected override void OnLeave(ISyntaxNode node, RewriteContext context)
    {
        context.Navigator.Pop();
        base.OnLeave(node, context);
    }

    protected override ObjectTypeDefinitionNode RewriteObjectTypeDefinition(
        ObjectTypeDefinitionNode node,
        RewriteContext context)
    {
        var originalName = node.Name.Value;

        node = base.RewriteObjectTypeDefinition(node, context);

        if (context.RenamedTypes.TryGetValue(originalName, out RenameInfo? _))
        {
            node = ApplyBindDirective(
                node,
                context,
                originalName,
                static (n, d) => n.WithDirectives(d));
        }

        return node;
    }

    protected override InterfaceTypeDefinitionNode RewriteInterfaceTypeDefinition(
        InterfaceTypeDefinitionNode node,
        RewriteContext context)
    {
        var originalName = node.Name.Value;

        node = base.RewriteInterfaceTypeDefinition(node, context);

        if (context.RenamedTypes.TryGetValue(originalName, out RenameInfo? _))
        {
            node = ApplyBindDirective(
                node,
                context,
                originalName,
                static (n, d) => n.WithDirectives(d));
        }

        return node;
    }

    protected override UnionTypeDefinitionNode RewriteUnionTypeDefinition(
        UnionTypeDefinitionNode node,
        RewriteContext context)
    {
        var originalName = node.Name.Value;

        node = base.RewriteUnionTypeDefinition(node, context);

        if (context.RenamedTypes.TryGetValue(originalName, out RenameInfo? _))
        {
            node = ApplyBindDirective(
                node,
                context,
                originalName,
                static (n, d) => n.WithDirectives(d));
        }

        return node;
    }

    protected override InputObjectTypeDefinitionNode RewriteInputObjectTypeDefinition(
        InputObjectTypeDefinitionNode node,
        RewriteContext context)
    {
        var originalName = node.Name.Value;

        node = base.RewriteInputObjectTypeDefinition(node, context);

        if (context.RenamedTypes.TryGetValue(originalName, out RenameInfo? _))
        {
            node = ApplyBindDirective(
                node,
                context,
                originalName,
                static (n, d) => n.WithDirectives(d));
        }

        return node;
    }

    protected override EnumTypeDefinitionNode RewriteEnumTypeDefinition(
        EnumTypeDefinitionNode node,
        RewriteContext context)
    {
        var originalName = node.Name.Value;

        node = base.RewriteEnumTypeDefinition(node, context);

        if (context.RenamedTypes.TryGetValue(originalName, out RenameInfo? _))
        {
            node = ApplyBindDirective(
                node,
                context,
                originalName,
                static (n, d) => n.WithDirectives(d));
        }

        return node;
    }

    protected override ScalarTypeDefinitionNode RewriteScalarTypeDefinition(
        ScalarTypeDefinitionNode node,
        RewriteContext context)
    {
        var originalName = node.Name.Value;

        node = base.RewriteScalarTypeDefinition(node, context);

        if (context.RenamedTypes.TryGetValue(originalName, out RenameInfo? _))
        {
            node = ApplyBindDirective(
                node,
                context,
                originalName,
                static (n, d) => n.WithDirectives(d));
        }

        return node;
    }

    protected override FieldDefinitionNode RewriteFieldDefinition(
        FieldDefinitionNode node,
        RewriteContext context)
    {
        node = base.RewriteFieldDefinition(node, context);

        return node;
    }

    protected override NameNode RewriteName(NameNode node, RewriteContext context)
    {
        if (!context.Navigator.TryPeek(1, out ISyntaxNode? parent))
        {
            return base.RewriteName(node, context);
        }

        if ((parent.Kind == SyntaxKind.ObjectTypeDefinition ||
            parent.Kind == SyntaxKind.InterfaceTypeDefinition ||
            parent.Kind == SyntaxKind.UnionTypeDefinition ||
            parent.Kind == SyntaxKind.InputObjectTypeDefinition ||
            parent.Kind == SyntaxKind.EnumTypeDefinition ||
            parent.Kind == SyntaxKind.ScalarTypeDefinition ||
            parent.Kind == SyntaxKind.NamedType) &&
            context.RenamedTypes.TryGetValue(node.Value, out RenameInfo? value))
        {
            return node.WithValue(value.Name);
        }

        if (context.Navigator.TryPeek(2, out ISyntaxNode? grandParent) &&
            grandParent.Kind == SyntaxKind.ObjectTypeDefinition &&
            parent.Kind == SyntaxKind.NamedType &&
            context.RenamedTypes.TryGetValue(node.Value, out value))
        {
            return node.WithValue(value.Name);
        }

        return base.RewriteName(node, context);
    }

    private T ApplyBindDirective<T>(
        T node,
        RewriteContext context,
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

        foreach (DirectiveNode directive in node.Directives)
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
