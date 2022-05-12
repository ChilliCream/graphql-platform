using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Rewriters;
using HotChocolate.Language.Rewriters.Contracts;
using HotChocolate.Language.Rewriters.Extensions;

namespace HotChocolate.Stitching.Types;

public class RenameFields<TContext> : Language.Rewriters.SchemaSyntaxRewriter<TContext>
{
    private readonly Dictionary<NameNode, List<ISyntaxReference>> _renames = new(SyntaxComparer.BySyntax);

    public RenameFields(IList<ISyntaxReference> renames)
    {
        foreach (ISyntaxReference rename in renames)
        {
            if (rename.Parent?.Node is not INamedSyntaxNode namedSyntaxNode)
            {
                throw new NotSupportedException();
            }

            NameNode oldName = namedSyntaxNode.Name;
            if (!_renames.TryGetValue(oldName, out List<ISyntaxReference>? references))
            {
                references = new List<ISyntaxReference>();
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
        if (!_renames.TryGetValue(name, out List<ISyntaxReference>? syntaxReferences))
        {
            return node;
        }

        ITypeDefinitionNode? typeDefinitionNode = navigator.GetAncestor<ITypeDefinitionNode>();

        if (!TryGetApplicableReference(
                syntaxReferences,
                typeDefinitionNode,
                navigator,
                out ISyntaxReference? applicableReference))
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
        List<ISyntaxReference> syntaxReferences,
        TParent? node,
        ISyntaxNavigator navigator,
        [MaybeNullWhen(false)] out ISyntaxReference syntaxReference)
        where TParent : ITypeDefinitionNode
    {
        if (node is null)
        {
            syntaxReference = default;
            return false;
        }

        foreach (ISyntaxReference reference in syntaxReferences)
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
        ISyntaxReference match,
        ISyntaxNavigator navigator,
        Func<TParent, NameNode, TParent> rewriteName,
        Func<TParent, IReadOnlyList<DirectiveNode>, TParent> rewriteDirectives)
        where TParent : INamedSyntaxNode
    {
        if (!RenameDirective.TryParse(match.Node, out RenameDirective? renameDirective))
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
