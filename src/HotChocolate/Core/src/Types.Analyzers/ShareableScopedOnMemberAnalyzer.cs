using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ShareableScopedOnMemberAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.ShareableScopedOnMember];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        AnalyzeMember(context, methodDeclaration.AttributeLists);
    }

    private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;
        AnalyzeMember(context, propertyDeclaration.AttributeLists);
    }

    private static void AnalyzeMember(
        SyntaxNodeAnalysisContext context,
        SyntaxList<AttributeListSyntax> attributeLists)
    {
        var semanticModel = context.SemanticModel;

        foreach (var attributeList in attributeLists)
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

                // Check for ShareableAttribute
                if (namedAttributeType.Name != "ShareableAttribute")
                {
                    continue;
                }

                var namespaceName = namedAttributeType.ContainingNamespace?.ToDisplayString();
                if (namespaceName != "HotChocolate.Types.Composite")
                {
                    continue;
                }

                // Check if the attribute has a "scoped" argument
                if (attribute.ArgumentList is not null)
                {
                    foreach (var argument in attribute.ArgumentList.Arguments)
                    {
                        // Check if this is a named argument with name "scoped"
                        if (argument.NameColon?.Name.Identifier.Text == "scoped"
                            || argument.NameEquals?.Name.Identifier.Text == "scoped")
                        {
                            var diagnostic = Diagnostic.Create(
                                Errors.ShareableScopedOnMember,
                                attribute.GetLocation());

                            context.ReportDiagnostic(diagnostic);
                            break;
                        }
                    }
                }
            }
        }
    }
}
