using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Stitching.Types.Pipeline.ApplyExtensions;

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

    protected override FieldDefinitionNode RewriteFieldDefinition(
        FieldDefinitionNode node,
        RewriteContext context)
    {
        node = base.RewriteFieldDefinition(node, context);

        return node;
    }

    protected override NameNode RewriteName(NameNode node, RewriteContext context)
    {
        if ((context.Navigator.Parent?.Kind == SyntaxKind.ObjectTypeDefinition ||
            context.Navigator.Parent?.Kind == SyntaxKind.InterfaceTypeDefinition ||
            context.Navigator.Parent?.Kind == SyntaxKind.NamedType) &&
            context.RenamedTypes.TryGetValue(node.Value, out RenameInfo? value))
        {
            return node.WithValue(value.Name);
        }

        context.Navigator.TryPeek(

        if ((context.Navigator.Parent?.Kind == SyntaxKind.ObjectTypeDefinition ||
            context.Navigator.Parent?.Kind == SyntaxKind.InterfaceTypeDefinition) &&
                context.Navigator.Parent?.Kind == SyntaxKind.NamedType) &&
            context.RenamedTypes.TryGetValue(node.Value, out RenameInfo? value))
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
