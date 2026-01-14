using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DataAttributeOrderAnalyzer : DiagnosticAnalyzer
{
    private const int UsePagingOrder = 0;
    private const int UseProjectionOrder = 1;
    private const int UseFilteringOrder = 2;
    private const int UseSortingOrder = 3;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.DataAttributeOrder];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        if (methodDeclaration.AttributeLists.Count == 0)
        {
            return;
        }

        // Find all data attributes and their positions
        var dataAttributes = new List<(AttributeSyntax Attribute, int Order, int Position)>();
        var position = 0;

        foreach (var attributeList in methodDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is not IMethodSymbol attributeSymbol)
                {
                    position++;
                    continue;
                }

                var attributeType = attributeSymbol.ContainingType;
                var order = GetDataAttributeOrder(attributeType);

                if (order >= 0)
                {
                    dataAttributes.Add((attribute, order, position));
                }

                position++;
            }
        }

        // Check if we have at least 2 data attributes to compare
        if (dataAttributes.Count < 2)
        {
            return;
        }

        // Check if the order is correct
        var previousOrder = -1;
        foreach (var (_, order, _) in dataAttributes)
        {
            if (order < previousOrder)
            {
                // Found an out-of-order attribute, report on the first one
                var diagnostic = Diagnostic.Create(
                    Errors.DataAttributeOrder,
                    dataAttributes[0].Attribute.GetLocation());

                context.ReportDiagnostic(diagnostic);
                return;
            }
            previousOrder = order;
        }
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
