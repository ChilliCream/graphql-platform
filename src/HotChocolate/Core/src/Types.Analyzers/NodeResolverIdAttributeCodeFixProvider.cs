using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NodeResolverIdAttributeCodeFixProvider))]
public sealed class NodeResolverIdAttributeCodeFixProvider : CodeFixProvider
{
    private const string Title = "Remove [ID] attribute";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["HC0092"];

    public override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the attribute identified by the diagnostic
        var attribute = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<AttributeSyntax>()
            .FirstOrDefault();

        if (attribute is null)
        {
            return;
        }

        // Register a code action that will invoke the fix
        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => RemoveAttributeAsync(
                    context.Document,
                    attribute,
                    c),
                equivalenceKey: Title),
            diagnostic);
    }

    private static async Task<Document> RemoveAttributeAsync(
        Document document,
        AttributeSyntax attribute,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return document;
        }

        // Get the attribute list containing this attribute
        if (attribute.Parent is not AttributeListSyntax attributeList)
        {
            return document;
        }

        SyntaxNode? newRoot;

        // If this is the only attribute in the list, remove the entire attribute list
        if (attributeList.Attributes.Count == 1)
        {
            newRoot = root.RemoveNode(attributeList, SyntaxRemoveOptions.KeepNoTrivia);
        }
        else
        {
            // Otherwise, just remove this attribute from the list
            var newAttributeList = attributeList.RemoveNode(attribute, SyntaxRemoveOptions.KeepNoTrivia);
            if (newAttributeList is null)
            {
                return document;
            }

            newRoot = root.ReplaceNode(attributeList, newAttributeList);
        }

        if (newRoot is null)
        {
            return document;
        }

        return document.WithSyntaxRoot(newRoot);
    }
}
