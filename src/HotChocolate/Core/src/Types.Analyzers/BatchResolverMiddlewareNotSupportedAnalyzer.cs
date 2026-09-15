using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static HotChocolate.Types.Analyzers.WellKnownAttributes;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BatchResolverMiddlewareNotSupportedAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.BatchResolverMiddlewareNotSupported];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        var isBatchResolver = false;

        foreach (var attributeList in methodDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                if (attributeSymbol.ContainingType.ToDisplayString().Equals(
                    BatchResolverAttribute,
                    StringComparison.Ordinal))
                {
                    isBatchResolver = true;
                    break;
                }
            }

            if (isBatchResolver)
            {
                break;
            }
        }

        if (!isBatchResolver)
        {
            return;
        }

        foreach (var attributeList in methodDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeType = attributeSymbol.ContainingType;
                var attributeFullName = attributeType.ToDisplayString();

                if (!PerParentMiddlewareAttributes.Contains(attributeFullName))
                {
                    continue;
                }

                var diagnostic = Diagnostic.Create(
                    Errors.BatchResolverMiddlewareNotSupported,
                    attribute.GetLocation(),
                    GetAttributeDisplayName(attributeType.Name),
                    methodDeclaration.Identifier.ValueText);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static string GetAttributeDisplayName(string attributeName)
        => attributeName.EndsWith("Attribute", StringComparison.Ordinal)
            ? attributeName.Substring(0, attributeName.Length - "Attribute".Length)
            : attributeName;
}
