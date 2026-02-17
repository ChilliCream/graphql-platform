using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static HotChocolate.Types.Analyzers.WellKnownAttributes;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NodeResolverPublicAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.NodeResolverMustBePublic];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not MethodDeclarationSyntax methodDeclaration)
        {
            return;
        }

        // Check if method has NodeResolver attribute
        if (!HasNodeResolverAttribute(context, methodDeclaration))
        {
            return;
        }

        // Check if method is public
        if (methodDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
        {
            return;
        }

        // Report diagnostic
        var diagnostic = Diagnostic.Create(
            Errors.NodeResolverMustBePublic,
            methodDeclaration.Identifier.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }

    private static bool HasNodeResolverAttribute(
        SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclaration)
    {
        if (methodDeclaration.AttributeLists.Count == 0)
        {
            return false;
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

                var attributeType = attributeSymbol.ContainingType.ToDisplayString();

                if (attributeType.Equals(NodeResolverAttribute, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
