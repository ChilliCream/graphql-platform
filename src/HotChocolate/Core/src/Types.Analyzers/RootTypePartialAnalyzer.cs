using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static HotChocolate.Types.Analyzers.WellKnownAttributes;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RootTypePartialAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.RootTypePartialKeywordMissing];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclaration)
        {
            return;
        }

        // Check if class is static
        if (!classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
        {
            return;
        }

        // Check if class is already partial
        if (classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            return;
        }

        // Check if class has any root type attributes
        if (!HasRootTypeAttribute(context, classDeclaration))
        {
            return;
        }

        // Report diagnostic
        var diagnostic = Diagnostic.Create(
            Errors.RootTypePartialKeywordMissing,
            classDeclaration.Identifier.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }

    private static bool HasRootTypeAttribute(
        SyntaxNodeAnalysisContext context,
        ClassDeclarationSyntax classDeclaration)
    {
        if (classDeclaration.AttributeLists.Count == 0)
        {
            return false;
        }

        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeType = attributeSymbol.ContainingType.ToDisplayString();

                if (attributeType.Equals(QueryTypeAttribute, StringComparison.Ordinal)
                    || attributeType.Equals(MutationTypeAttribute, StringComparison.Ordinal)
                    || attributeType.Equals(SubscriptionTypeAttribute, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
