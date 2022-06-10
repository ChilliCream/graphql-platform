using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Utilities;
using static System.Array;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyExtensions;

public sealed class ApplyRenamingMiddleware
{
    private const string _schema = "$schema";

    private readonly RenameRewriter _rewriter = new();
    private readonly MergeSchema _next;

    public ApplyRenamingMiddleware(MergeSchema next)
    {
        _next = next;
    }

    public async ValueTask InvokeAsync(ISchemaMergeContext context)
    {
        foreach (Document document in context.Documents)
        {
            var rewriteContext = new RewriteContext(document.Name);
            _rewriter.Rewrite(document.SyntaxTree, rewriteContext);
        }

        await _next(context);
    }

    private sealed class RewriteContext : INavigatorContext
    {
        public RewriteContext(string sourceName)
        {
            SourceName = sourceName;
        }

        public string SourceName { get; }

        public ISyntaxNavigator Navigator { get; } = new DefaultSyntaxNavigator();
    }

    private sealed class RenameRewriter : SyntaxRewriter<RewriteContext>
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
                if (node.Directives.Count > 0)
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
                else
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
            if (TryRename(node.Directives, context, out var directive, out var @as))
            {
                var directives = node.Directives.ToList();
                directives.Remove(directive);
                directives.Add(new BindDirective(context.SourceName, node.Name.Value));
                rewritten = createNode(new NameNode(@as), directives, node);
                return true;
            }

            rewritten = node;
            return false;
        }

        private bool TryRename(
            IReadOnlyList<DirectiveNode> directives,
            RewriteContext context,
            [NotNullWhen(true)] out DirectiveNode? directive,
            [NotNullWhen(true)] out string? @as)
        {
            if (directives.Count == 0)
            {
                directive = null;
                @as = null;
                return false;
            }

            for (var i = 0; i < directives.Count; i++)
            {
                DirectiveNode node = directives[0];

                if (node.Name.Value.EqualsOrdinal("rename"))
                {
                    if (node.Arguments.Count != 1)
                    {
                        throw ThrowHelper.RenameDirectiveInvalidStructure(
                            context.Navigator.CreateCoordinate());
                    }

                    ArgumentNode argument = node.Arguments[0];
                    if (!argument.Name.Value.EqualsOrdinal("to") ||
                        argument.Value is not StringValueNode toValue ||
                        !NameUtils.IsValidGraphQLName(toValue.Value))
                    {
                        throw ThrowHelper.RenameDirectiveInvalidStructure(
                            context.Navigator.CreateCoordinate());
                    }

                    directive = node;
                    @as = toValue.Value;
                    return true;
                }
            }

            directive = null;
            @as = null;
            return false;
        }
    }
}
