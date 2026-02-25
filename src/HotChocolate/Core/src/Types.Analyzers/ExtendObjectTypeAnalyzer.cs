using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExtendObjectTypeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.ExtendObjectTypeShouldBeUpgraded];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        if (classDeclaration.AttributeLists.Count == 0)
        {
            return;
        }

        var semanticModel = context.SemanticModel;

        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeType = attributeSymbol.ContainingType;

                // Check if this is ExtendObjectTypeAttribute
                if (attributeType is not INamedTypeSymbol namedAttributeType
                    || namedAttributeType.Name != "ExtendObjectTypeAttribute"
                    || namedAttributeType.ContainingNamespace?.ToDisplayString() != "HotChocolate.Types")
                {
                    continue;
                }

                // Check if it's a generic attribute and extract the type argument
                if (namedAttributeType.IsGenericType && namedAttributeType.TypeArguments.Length == 1)
                {
                    var typeArgument = namedAttributeType.TypeArguments[0];
                    var diagnostic = Diagnostic.Create(
                        Errors.ExtendObjectTypeShouldBeUpgraded,
                        attribute.GetLocation(),
                        typeArgument.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
