using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DataLoaderReturnTypeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.DataLoaderReturnTypeInvalid];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(
            methodDeclaration,
            context.CancellationToken);

        if (methodSymbol is null)
        {
            return;
        }

        var attributes = GenericDataLoaderAnalyzerHelper.GetDataLoaderAttributes(
            context.SemanticModel,
            methodDeclaration,
            context.Compilation,
            context.CancellationToken);

        foreach (var attribute in attributes.Where(t => t.IsGeneric))
        {
            if (attribute.Type.TypeArguments[0] is not INamedTypeSymbol dataLoaderType
                || !GenericDataLoaderAnalyzerHelper.TryResolveContract(
                    dataLoaderType,
                    context.Compilation,
                    out var contract)
                || GenericDataLoaderAnalyzerHelper.HasValidReturnType(
                    methodSymbol,
                    contract,
                    context.Compilation))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Errors.DataLoaderReturnTypeInvalid,
                methodDeclaration.ReturnType.GetLocation(),
                contract.Kind is DataLoaderKind.Batch
                    ? "Task/ValueTask of IReadOnlyDictionary<TKey, TValue> or IDictionary<TKey, TValue>"
                    : "Task/ValueTask of TValue"));
        }
    }
}
