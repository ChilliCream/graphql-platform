using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class QueryContextProjectionAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.QueryContextWithUseProjection];

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

        // Check if the method has any QueryContext<T> parameters
        var hasQueryContext = false;
        foreach (var parameter in methodDeclaration.ParameterList.Parameters)
        {
            if (parameter.Type is null)
            {
                continue;
            }

            var typeInfo = semanticModel.GetTypeInfo(parameter.Type);
            if (typeInfo.Type is INamedTypeSymbol namedType && IsQueryContext(namedType))
            {
                hasQueryContext = true;
                break;
            }
        }

        if (!hasQueryContext)
        {
            return;
        }

        // Check for UseProjection or UseProjections attribute
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
                if (IsUseProjectionAttribute(attributeType))
                {
                    var diagnostic = Diagnostic.Create(
                        Errors.QueryContextWithUseProjection,
                        attribute.GetLocation());

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static bool IsQueryContext(INamedTypeSymbol type)
    {
        // Check if this is QueryContext<T>
        if (type.Name != "QueryContext")
        {
            return false;
        }

        var namespaceName = type.ContainingNamespace?.ToDisplayString();
        return namespaceName == "GreenDonut.Data";
    }

    private static bool IsUseProjectionAttribute(INamedTypeSymbol attributeType)
    {
        // Check for UseProjectionAttribute or UseProjectionsAttribute
        var attributeName = attributeType.Name;
        var namespaceName = attributeType.ContainingNamespace?.ToDisplayString();

        if (namespaceName != "HotChocolate.Data")
        {
            return false;
        }

        return attributeName is "UseProjectionAttribute" or "UseProjectionsAttribute";
    }
}
