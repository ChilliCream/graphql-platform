using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DataLoaderKeyedServiceAttributeIgnoredCodeFixProvider))]
public sealed class DataLoaderKeyedServiceAttributeIgnoredCodeFixProvider : CodeFixProvider
{
    private const string Title = "Remove keyed service attribute";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [ErrorCodes.Analyzers.DataLoaderKeyedServiceAttributeIgnored];

    public override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return;
        }

        var attribute = root.FindNode(context.Diagnostics[0].Location.SourceSpan)
            .AncestorsAndSelf()
            .OfType<AttributeSyntax>()
            .FirstOrDefault();

        if (attribute is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                c => RemoveAttributeAsync(context.Document, attribute, c),
                Title),
            context.Diagnostics);
    }

    private static async Task<Document> RemoveAttributeAsync(
        Document document,
        AttributeSyntax attribute,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root is null || attribute.Parent is not AttributeListSyntax attributeList)
        {
            return document;
        }

        SyntaxNode? newRoot;

        if (attributeList.Attributes.Count == 1)
        {
            newRoot = root.RemoveNode(attributeList, SyntaxRemoveOptions.KeepNoTrivia);
        }
        else
        {
            var newAttributeList = attributeList.RemoveNode(attribute, SyntaxRemoveOptions.KeepNoTrivia);
            newRoot = newAttributeList is null ? null : root.ReplaceNode(attributeList, newAttributeList);
        }

        return newRoot is null ? document : document.WithSyntaxRoot(newRoot);
    }
}
