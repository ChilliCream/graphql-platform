using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ShareableInterfaceTypeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.ShareableOnInterfaceType];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        AttributeSyntax? interfaceTypeAttribute = null;
        AttributeSyntax? shareableAttribute = null;

        // Check class attributes
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
                if (attributeType is not INamedTypeSymbol namedAttributeType)
                {
                    continue;
                }

                var namespaceName = namedAttributeType.ContainingNamespace?.ToDisplayString();

                // Check for InterfaceTypeAttribute
                if (namedAttributeType.Name == "InterfaceTypeAttribute"
                    && namespaceName == "HotChocolate.Types")
                {
                    interfaceTypeAttribute = attribute;
                }

                // Check for ShareableAttribute
                if (namedAttributeType.Name == "ShareableAttribute"
                    && namespaceName == "HotChocolate.Types.Composite")
                {
                    shareableAttribute = attribute;
                }
            }
        }

        // Report error if both attributes are present
        if (interfaceTypeAttribute is not null && shareableAttribute is not null)
        {
            var diagnostic = Diagnostic.Create(
                Errors.ShareableOnInterfaceType,
                shareableAttribute.GetLocation());

            context.ReportDiagnostic(diagnostic);
        }
    }
}
