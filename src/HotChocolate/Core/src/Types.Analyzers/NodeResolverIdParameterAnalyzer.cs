using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NodeResolverIdParameterAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.NodeResolverIdParameter];

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

        // Check if method has [NodeResolver] attribute
        var hasNodeResolverAttribute = false;

        foreach (var attributeList in methodDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeType = attributeSymbol.ContainingType;
                if (attributeType is not INamedTypeSymbol namedAttributeType)
                {
                    continue;
                }

                if (namedAttributeType.Name == "NodeResolverAttribute"
                    && namedAttributeType.ContainingNamespace?.ToDisplayString() == "HotChocolate.Types.Relay")
                {
                    hasNodeResolverAttribute = true;
                    break;
                }
            }

            if (hasNodeResolverAttribute)
            {
                break;
            }
        }

        if (!hasNodeResolverAttribute)
        {
            return;
        }

        // Check if method has parameters
        if (methodDeclaration.ParameterList.Parameters.Count == 0)
        {
            return;
        }

        // Check if first parameter is named "id"
        var firstParameter = methodDeclaration.ParameterList.Parameters[0];
        if (firstParameter.Identifier.Text != "id")
        {
            var diagnostic = Diagnostic.Create(
                Errors.NodeResolverIdParameter,
                firstParameter.GetLocation());

            context.ReportDiagnostic(diagnostic);
        }
    }
}
