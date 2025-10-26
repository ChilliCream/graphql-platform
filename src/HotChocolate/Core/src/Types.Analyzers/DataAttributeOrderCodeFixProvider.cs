using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DataAttributeOrderCodeFixProvider))]
public sealed class DataAttributeOrderCodeFixProvider : CodeFixProvider
{
    private const int UsePagingOrder = 0;
    private const int UseProjectionOrder = 1;
    private const int UseFilteringOrder = 2;
    private const int UseSortingOrder = 3;

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["HC0100"];

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

        // Find the method declaration
        var node = root.FindNode(diagnosticSpan);
        var methodDeclaration = node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodDeclaration is null)
        {
            return;
        }

        const string title = "Reorder data attributes";

        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: c => ReorderAttributesAsync(
                    context.Document,
                    methodDeclaration,
                    c),
                equivalenceKey: title),
            diagnostic);
    }

    private static async Task<Document> ReorderAttributesAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
            return document;
        }

        // Collect all attributes with their metadata
        var allAttributes = new List<(AttributeSyntax Attribute, AttributeListSyntax List, int Order, bool IsDataAttribute)>();

        foreach (var attributeList in methodDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is IMethodSymbol attributeSymbol)
                {
                    var attributeType = attributeSymbol.ContainingType;
                    var order = GetDataAttributeOrder(attributeType);
                    var isDataAttribute = order >= 0;
                    allAttributes.Add((attribute, attributeList, order, isDataAttribute));
                }
                else
                {
                    // Unknown attribute, treat as non-data attribute
                    allAttributes.Add((attribute, attributeList, -1, false));
                }
            }
        }

        // Separate data attributes from non-data attributes
        var dataAttributes = allAttributes
            .Where(a => a.IsDataAttribute)
            .OrderBy(a => a.Order)
            .ToList();

        var nonDataAttributes = allAttributes
            .Where(a => !a.IsDataAttribute)
            .ToList();

        // Build new attribute lists
        var newAttributeLists = new List<AttributeListSyntax>();

        // Add data attributes first (each in their own list to preserve formatting)
        foreach (var (attribute, originalList, _, _) in dataAttributes)
        {
            var newList = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(attribute.WithoutTrivia()))
                .WithLeadingTrivia(originalList.GetLeadingTrivia())
                .WithTrailingTrivia(originalList.GetTrailingTrivia());
            newAttributeLists.Add(newList);
        }

        // Add non-data attributes in their original relative order
        var processedLists = new HashSet<AttributeListSyntax>();
        foreach (var (attribute, originalList, _, _) in nonDataAttributes)
        {
            if (processedLists.Contains(originalList))
            {
                // Already added this list
                continue;
            }

            // Get all non-data attributes from this list
            var attributesInList = nonDataAttributes
                .Where(a => a.List == originalList)
                .Select(a => a.Attribute.WithoutTrivia())
                .ToList();

            if (attributesInList.Count > 0)
            {
                var newList = SyntaxFactory.AttributeList(
                    SyntaxFactory.SeparatedList(attributesInList))
                    .WithLeadingTrivia(originalList.GetLeadingTrivia())
                    .WithTrailingTrivia(originalList.GetTrailingTrivia());
                newAttributeLists.Add(newList);
                processedLists.Add(originalList);
            }
        }

        // Create new method with reordered attributes
        var newMethod = methodDeclaration.WithAttributeLists(
            SyntaxFactory.List(newAttributeLists));

        var newRoot = root.ReplaceNode(methodDeclaration, newMethod);
        return document.WithSyntaxRoot(newRoot);
    }

    private static int GetDataAttributeOrder(INamedTypeSymbol attributeType)
    {
        var attributeName = attributeType.Name;
        var namespaceName = attributeType.ContainingNamespace?.ToDisplayString();

        // UsePaging is in HotChocolate.Types
        if (namespaceName == "HotChocolate.Types" && attributeName == "UsePagingAttribute")
        {
            return UsePagingOrder;
        }

        // UseProjection, UseFiltering, UseSorting are in HotChocolate.Data
        if (namespaceName == "HotChocolate.Data")
        {
            return attributeName switch
            {
                "UseProjectionAttribute" => UseProjectionOrder,
                "UseProjectionsAttribute" => UseProjectionOrder,
                "UseFilteringAttribute" => UseFilteringOrder,
                "UseSortingAttribute" => UseSortingOrder,
                _ => -1
            };
        }

        return -1;
    }
}
