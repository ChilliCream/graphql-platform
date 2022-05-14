using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Rewriters;
using HotChocolate.Language.Rewriters.Contracts;
using HotChocolate.Stitching.Types.Directives;
using HotChocolate.Stitching.Types.Extensions;

namespace HotChocolate.Stitching.Types.Rewriters;

public class RenameFields<TContext> : Language.Rewriters.SchemaSyntaxRewriter<TContext>
{
    private readonly Dictionary<NameNode, List<SyntaxReference>> _renames = new(SyntaxComparer.BySyntax);

    public RenameFields(IReadOnlyList<SyntaxReference> renames)
    {
        foreach (SyntaxReference rename in renames)
        {
            if (rename.GetParent() is not INamedSyntaxNode namedSyntaxNode)
            {
                throw new NotSupportedException();
            }

            NameNode oldName = namedSyntaxNode.Name;
            if (!_renames.TryGetValue(oldName, out List<SyntaxReference>? references))
            {
                references = new List<SyntaxReference>();
                _renames.Add(oldName, references);
            }

            references.Add(rename);
        }
    }

    protected override FieldDefinitionNode RewriteFieldDefinition(
        FieldDefinitionNode node,
        ISyntaxNavigator navigator,
        TContext context)
    {
        node = base.RewriteFieldDefinition(node, navigator, context);

        return RewriteField(
            node,
            node.Name,
            navigator,
            (_, name) => _.WithName(name),
            (_, directives) => _.WithDirectives(directives));
    }

    private TParent RewriteField<TParent>(
        TParent node,
        NameNode name,
        ISyntaxNavigator navigator,
        Func<TParent, NameNode, TParent> rewriteName,
        Func<TParent, IReadOnlyList<DirectiveNode>, TParent> rewriteDirectives)
        where TParent : INamedSyntaxNode
    {
        if (!_renames.TryGetValue(name, out List<SyntaxReference>? syntaxReferences))
        {
            return node;
        }

        ITypeDefinitionNode? typeDefinitionNode = navigator.GetAncestor<ITypeDefinitionNode>();

        if (!TryGetApplicableReference(
                syntaxReferences,
                typeDefinitionNode,
                navigator,
                out SyntaxReference? applicableReference))
        {
            return node;
        }

        return Rewrite(node,
            applicableReference,
            navigator,
            rewriteName,
            rewriteDirectives);
    }

    private bool TryGetApplicableReference<TParent>(
        List<SyntaxReference> syntaxReferences,
        TParent? node,
        ISyntaxNavigator navigator,
        [MaybeNullWhen(false)] out SyntaxReference syntaxReference)
        where TParent : ITypeDefinitionNode
    {
        if (node is null)
        {
            syntaxReference = default;
            return false;
        }

        foreach (SyntaxReference reference in syntaxReferences)
        {
            ITypeDefinitionNode? typeDefinition = reference.GetAncestor<ITypeDefinitionNode>();
            switch (typeDefinition)
            {
                // If the rename node originated on an interface type, we need to rename the field on a type that implements the interface.
                case InterfaceTypeDefinitionNode interfaceTypeDefinition:

                    switch (node)
                    {
                        // The target field is part of the interface.
                        case InterfaceTypeDefinitionNode:
                            syntaxReference = reference;
                            return true;

                        // The target field is on a type, make sure the type inherits from the interface where the rename directive was specified.
                        case ObjectTypeDefinitionNode objectTypeDefinition:
                            if (!objectTypeDefinition.Inherits(interfaceTypeDefinition))
                            {
                                continue;
                            }

                            syntaxReference = reference;
                            return true;
                    }

                    break;

                // Some other type definition make sure the names match.
                case { }:

                    ITypeDefinitionNode typeDefinitionNode = navigator.GetAncestor<ITypeDefinitionNode>()!;
                    if (!SyntaxComparer.BySyntax.Equals(typeDefinition.Name, typeDefinitionNode.Name))
                    {
                        continue;
                    }

                    syntaxReference = reference;
                    return true;
            }
        }

        syntaxReference = default;
        return false;
    }

    private static TParent Rewrite<TParent>(TParent node,
        SyntaxReference match,
        ISyntaxNavigator navigator,
        Func<TParent, NameNode, TParent> rewriteName,
        Func<TParent, IReadOnlyList<DirectiveNode>, TParent> rewriteDirectives)
        where TParent : INamedSyntaxNode
    {
        if (match.Node is not RenameDirective renameDirective)
        {
            return node;
        }

        SchemaCoordinate coordinate = navigator.CreateCoordinate();
        TParent renamedNode = rewriteName(node, renameDirective.NewName);

        IReadOnlyList<DirectiveNode> directives = node.Directives
            .ReplaceOrAddDirective(renameDirective.Directive, SourceDirective.Create(coordinate));

        return rewriteDirectives(renamedNode, directives);
    }
}
