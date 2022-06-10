using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Stitching.Types.Pipeline.ApplyExtensions;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyRenaming;

internal sealed class RenameRewriter : SyntaxRewriter<RewriteContext>
{
    protected override ObjectTypeDefinitionNode RewriteObjectTypeDefinition(
        ObjectTypeDefinitionNode node,
        RewriteContext context)
    {
        node = base.RewriteObjectTypeDefinition(node, context);

        TryRenameNode(
            node,
            context,
            static (n, d, o)
                => new ObjectTypeDefinitionNode(
                    o.Location,
                    n,
                    o.Description,
                    d,
                    o.Interfaces,
                    o.Fields),
            out node);

        return node;
    }

    protected override InterfaceTypeDefinitionNode RewriteInterfaceTypeDefinition(
        InterfaceTypeDefinitionNode node,
        RewriteContext context)
    {
        node = base.RewriteInterfaceTypeDefinition(node, context);

        TryRenameNode(
            node,
            context,
            static (n, d, o)
                => new InterfaceTypeDefinitionNode(
                    o.Location,
                    n,
                    o.Description,
                    d,
                    o.Interfaces,
                    o.Fields),
            out node);

        return node;
    }

    protected override FieldDefinitionNode RewriteFieldDefinition(
        FieldDefinitionNode node,
        RewriteContext context)
    {
        node = base.RewriteFieldDefinition(node, context);

        if (!TryRenameNode(
            node,
            context,
            static (n, d, o)
                => new FieldDefinitionNode(
                    o.Location,
                    n,
                    o.Description,
                    o.Arguments,
                    o.Type,
                    d),
            out node))
        {
            if(node.Directives.Count == 0)
            {
                var directives = new DirectiveNode[]
                {
                    new BindDirective(context.SourceName)
                };

                return new FieldDefinitionNode(
                    node.Location,
                    node.Name,
                    node.Description,
                    node.Arguments,
                    node.Type,
                    directives);
            }

            if (node.Directives.All(static t => !BindDirective.IsOfType(t)))
            {
                var directives = node.Directives.ToList();
                directives.Add(new BindDirective(context.SourceName));
                return new FieldDefinitionNode(
                    node.Location,
                    node.Name,
                    node.Description,
                    node.Arguments,
                    node.Type,
                    directives);
            }
        }

        return node;
    }

    private bool TryRenameNode<T>(
        T node,
        RewriteContext context,
        Func<NameNode, IReadOnlyList<DirectiveNode>, T, T> createNode,
        out T rewritten)
        where T : INamedSyntaxNode
    {
        if (TryRename(node.Directives, context, out var directive, out var to))
        {
            var directives = node.Directives.ToList();
            directives.Remove(directive);
            directives.Add(new BindDirective(context.SourceName, node.Name.Value));
            rewritten = createNode(new NameNode(to), directives, node);
            return true;
        }

        rewritten = node;
        return false;
    }

    private bool TryRename(
        IReadOnlyList<DirectiveNode> directives,
        RewriteContext context,
        [NotNullWhen(true)] out DirectiveNode? directive,
        [NotNullWhen(true)] out string? to)
    {
        if (directives.Count == 0)
        {
            directive = null;
            to = null;
            return false;
        }

        for (var i = 0; i < directives.Count; i++)
        {
            DirectiveNode node = directives[0];
            if (RenameDirective.TryParse(node, out RenameDirective? rename))
            {
                directive = node;
                to = rename.To;
                return true;
            }
        }

        directive = null;
        to = null;
        return false;
    }
}
