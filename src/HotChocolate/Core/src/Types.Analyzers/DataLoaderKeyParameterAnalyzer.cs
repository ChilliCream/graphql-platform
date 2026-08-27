using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DataLoaderKeyParameterAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.DataLoaderKeyParameterInvalid];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);

        if (methodSymbol is null)
        {
            return;
        }

        var attributes = GenericDataLoaderAnalyzerHelper.GetDataLoaderAttributes(
            context.SemanticModel,
            methodDeclaration,
            context.Compilation);

        foreach (var attribute in attributes.Where(t => t.IsGeneric))
        {
            if (attribute.Type.TypeArguments[0] is not INamedTypeSymbol dataLoaderType
                || !GenericDataLoaderAnalyzerHelper.TryResolveContract(
                    dataLoaderType,
                    context.Compilation,
                    out var contract)
                || GenericDataLoaderAnalyzerHelper.HasValidKeyParameter(
                    methodSymbol,
                    contract,
                    context.Compilation))
            {
                continue;
            }

            var location = methodDeclaration.ParameterList.Parameters.Count > 0
                ? methodDeclaration.ParameterList.Parameters[0].Type?.GetLocation()
                    ?? methodDeclaration.ParameterList.Parameters[0].GetLocation()
                : methodDeclaration.Identifier.GetLocation();

            context.ReportDiagnostic(Diagnostic.Create(
                Errors.DataLoaderKeyParameterInvalid,
                location,
                contract.Kind is DataLoaderKind.Batch ? "IReadOnlyList<TKey>" : "TKey"));
        }
    }
}
