using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static HotChocolate.Types.Analyzers.WellKnownAttributes;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BatchResolverOnMutationFieldAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.BatchResolverOnMutationField];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        if (!HasAttribute(context, methodDeclaration.AttributeLists, BatchResolverAttribute))
        {
            return;
        }

        var isMutationField = HasAttribute(context, methodDeclaration.AttributeLists, MutationAttribute);

        if (!isMutationField
            && methodDeclaration.Parent is TypeDeclarationSyntax containingType)
        {
            isMutationField = HasAttribute(context, containingType.AttributeLists, MutationTypeAttribute);
        }

        if (!isMutationField)
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            Errors.BatchResolverOnMutationField,
            methodDeclaration.Identifier.GetLocation(),
            methodDeclaration.Identifier.ValueText);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool HasAttribute(
        SyntaxNodeAnalysisContext context,
        SyntaxList<AttributeListSyntax> attributeLists,
        string attributeFullName)
    {
        foreach (var attributeList in attributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                if (attributeSymbol.ContainingType.ToDisplayString().Equals(
                    attributeFullName,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
