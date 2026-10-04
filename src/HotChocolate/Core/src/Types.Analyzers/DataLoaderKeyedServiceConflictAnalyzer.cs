using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DataLoaderKeyedServiceConflictAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.DataLoaderKeyedServiceAttributeConflict];

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

        if (attributes.IsEmpty)
        {
            return;
        }

        foreach (var parameter in methodDeclaration.ParameterList.Parameters)
        {
            if (parameter.AttributeLists.Count == 0
                || context.SemanticModel.GetDeclaredSymbol(parameter, context.CancellationToken)
                    is not IParameterSymbol parameterSymbol)
            {
                continue;
            }

            var serviceAttributeInfo = parameterSymbol.GetServiceAttributeInfo(context.Compilation);

            if (serviceAttributeInfo.HasServiceAttribute
                && serviceAttributeInfo.HasFromKeyedServicesAttribute)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Errors.DataLoaderKeyedServiceAttributeConflict,
                    Location.Create(
                        parameter.SyntaxTree,
                        TextSpan.FromBounds(
                            parameter.AttributeLists[0].SpanStart,
                            parameter.AttributeLists[parameter.AttributeLists.Count - 1].Span.End))));
            }
        }
    }
}
