using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(QueryContextProjectionCodeFixProvider))]
public sealed class QueryContextProjectionCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["HC0099"];

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
        var attribute = node.AncestorsAndSelf().OfType<AttributeSyntax>().FirstOrDefault();
        if (attribute is null)
        {
            return;
        }

        const string title = "Remove [UseProjection] attribute";

        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: c => RemoveAttributeAsync(
                    context.Document,
                    attribute,
                    c),
                equivalenceKey: title),
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

        // Get the attribute list that contains this attribute
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
            // Remove just this attribute from the list
            var newAttributes = attributeList.Attributes.Remove(attribute);
            var newAttributeList = attributeList.WithAttributes(newAttributes);
            newRoot = root.ReplaceNode(attributeList, newAttributeList);
        }

        if (newRoot is null)
        {
            return document;
        }

        return document.WithSyntaxRoot(newRoot);
    }
}
