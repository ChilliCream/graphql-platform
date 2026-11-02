using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DataLoaderKeyedServiceKeyNotDeterminableAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.DataLoaderKeyedServiceKeyNotDeterminable];

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

        if (attributes.IsEmpty
            || context.SemanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken)
                is not IMethodSymbol methodSymbol
            || methodSymbol.Parameters.IsEmpty)
        {
            return;
        }

        var parameters = DataLoaderInfo.CreateParameters(methodSymbol, context.Compilation);

        foreach (var parameter in parameters)
        {
            if (parameter.Kind is not DataLoaderParameterKind.Service)
            {
                continue;
            }

            var serviceAttributeInfo = parameter.Parameter.GetServiceAttributeInfo(context.Compilation);

            if (!serviceAttributeInfo.IsServiceKeyUndeterminable)
            {
                continue;
            }

            foreach (var attribute in parameter.Parameter.GetAttributes())
            {
                if (attribute.AttributeClass is null
                    || attribute.AttributeClass.ToDisplayString() == WellKnownAttributes.ServiceAttribute
                    || !attribute.AttributeClass.IsOrInheritsFrom(WellKnownAttributes.ServiceAttribute)
                    || attribute.GetDerivedServiceKey(context.Compilation, out _)
                        is not SymbolExtensions.ServiceKeyExtractionResult.Undeterminable)
                {
                    continue;
                }

                var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken)
                    .GetLocation()
                    ?? parameter.Parameter.Locations.FirstOrDefault()
                    ?? Location.None;

                context.ReportDiagnostic(Diagnostic.Create(
                    Errors.DataLoaderKeyedServiceKeyNotDeterminable,
                    location));
            }
        }
    }
}
