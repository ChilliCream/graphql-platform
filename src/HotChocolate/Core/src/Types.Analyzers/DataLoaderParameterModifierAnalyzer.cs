using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DataLoaderParameterModifierAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.DataLoaderParameterModifierInvalid];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        var attributes = GenericDataLoaderAnalyzerHelper.GetDataLoaderAttributes(
            context.SemanticModel,
            methodDeclaration,
            context.Compilation,
            context.CancellationToken);

        if (!attributes.Any(t => t.IsGeneric))
        {
            return;
        }

        foreach (var parameter in methodDeclaration.ParameterList.Parameters)
        {
            if (!parameter.Modifiers.Any(t => t.IsKind(SyntaxKind.RefKeyword)
                || t.IsKind(SyntaxKind.InKeyword)
                || t.IsKind(SyntaxKind.OutKeyword)))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Errors.DataLoaderParameterModifierInvalid,
                Location.Create(methodDeclaration.SyntaxTree, parameter.Modifiers.Span)));
        }
    }
}
