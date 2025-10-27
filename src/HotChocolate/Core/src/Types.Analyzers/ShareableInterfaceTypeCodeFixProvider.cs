using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ShareableInterfaceTypeCodeFixProvider))]
public sealed class ShareableInterfaceTypeCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["HC0102"];

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

        // Find the attribute syntax
        var node = root.FindNode(diagnosticSpan);
        var attributeSyntax = node.AncestorsAndSelf().OfType<AttributeSyntax>().FirstOrDefault();
        if (attributeSyntax is null)
        {
            return;
        }

        const string title = "Remove [Shareable] attribute";

        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: c => RemoveShareableAttributeAsync(
                    context.Document,
                    attributeSyntax,
                    c),
                equivalenceKey: title),
            diagnostic);
    }

    private static async Task<Document> RemoveShareableAttributeAsync(
        Document document,
        AttributeSyntax attribute,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        // Find the attribute list that contains this attribute
        var attributeList = attribute.Parent as AttributeListSyntax;
        if (attributeList is null)
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
            // Otherwise, just remove this specific attribute from the list
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
