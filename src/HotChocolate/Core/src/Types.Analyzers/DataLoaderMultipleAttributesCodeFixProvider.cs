using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DataLoaderMultipleAttributesCodeFixProvider))]
public sealed class DataLoaderMultipleAttributesCodeFixProvider : CodeFixProvider
{
    private const string Title = "Remove duplicate DataLoader attribute";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [ErrorCodes.Analyzers.DataLoaderMultipleAttributes];

    public override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
            .ConfigureAwait(false);

        if (root is null
            || semanticModel is null
            || root.FindNode(context.Diagnostics[0].Location.SourceSpan)
                .AncestorsAndSelf()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault() is not { } methodDeclaration)
        {
            return;
        }

        var attributes = GenericDataLoaderAnalyzerHelper.GetDataLoaderAttributes(
            semanticModel,
            methodDeclaration,
            semanticModel.Compilation,
            context.CancellationToken);

        if (attributes.Length <= 1)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                c => RemoveDuplicateAttributesAsync(context.Document, methodDeclaration, attributes, c),
                Title),
            context.Diagnostics);
    }

    private static async Task<Document> RemoveDuplicateAttributesAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        ImmutableArray<DataLoaderAttributeInfo> attributes,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return document;
        }

        var retainedAttribute = attributes.Where(t => t.IsGeneric).LastOrDefault().Syntax
            ?? attributes[attributes.Length - 1].Syntax;
        var removedAttributes = new HashSet<AttributeSyntax>(
            attributes.Where(t => t.Syntax != retainedAttribute).Select(t => t.Syntax));
        SyntaxNode newRoot = root;

        foreach (var attributeList in methodDeclaration.AttributeLists)
        {
            var dataLoaderAttributes = attributeList.Attributes
                .Where(removedAttributes.Contains)
                .ToArray();

            if (dataLoaderAttributes.Length == 0)
            {
                continue;
            }

            if (dataLoaderAttributes.Length == attributeList.Attributes.Count)
            {
                newRoot = newRoot.RemoveNode(attributeList, SyntaxRemoveOptions.KeepExteriorTrivia) ?? newRoot;
            }
            else
            {
                var newAttributes = new List<AttributeSyntax>(attributeList.Attributes.Count);

                for (var i = 0; i < attributeList.Attributes.Count; i++)
                {
                    var attribute = attributeList.Attributes[i];

                    if (removedAttributes.Contains(attribute))
                    {
                        continue;
                    }

                    if (attribute == retainedAttribute)
                    {
                        if (attributeList.Attributes.Take(i).Any(removedAttributes.Contains))
                        {
                            attribute = attribute.WithLeadingTrivia(RemoveWhitespace(attribute.GetLeadingTrivia()));
                        }

                        if (attributeList.Attributes.Skip(i + 1).Any(removedAttributes.Contains))
                        {
                            attribute = attribute.WithTrailingTrivia(RemoveWhitespace(attribute.GetTrailingTrivia()));
                        }
                    }

                    newAttributes.Add(attribute);
                }

                newRoot = newRoot.ReplaceNode(
                    attributeList,
                    attributeList.WithAttributes(SyntaxFactory.SeparatedList(newAttributes)));
            }
        }

        return document.WithSyntaxRoot(newRoot);
    }

    private static SyntaxTriviaList RemoveWhitespace(SyntaxTriviaList trivia)
        => SyntaxFactory.TriviaList(trivia.Where(t => !t.IsKind(SyntaxKind.WhitespaceTrivia)));
}
