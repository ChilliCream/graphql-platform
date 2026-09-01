using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DataLoaderMultipleAttributesAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.DataLoaderMultipleAttributes];

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

        if (attributes.Length <= 1)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Errors.DataLoaderMultipleAttributes,
            attributes[1].Syntax.GetLocation()));
    }
}
