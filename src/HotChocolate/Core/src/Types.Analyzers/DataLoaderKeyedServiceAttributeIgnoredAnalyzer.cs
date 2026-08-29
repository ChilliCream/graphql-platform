using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DataLoaderKeyedServiceAttributeIgnoredAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.DataLoaderKeyedServiceAttributeIgnored];

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

        foreach (var parameter in methodDeclaration.ParameterList.Parameters)
        {
            if (context.SemanticModel.GetDeclaredSymbol(parameter, context.CancellationToken)
                is not IParameterSymbol parameterSymbol
                || parameters.FirstOrDefault(t => SymbolEqualityComparer.Default.Equals(
                    t.Parameter,
                    parameterSymbol)) is not { Kind: not DataLoaderParameterKind.Service })
            {
                continue;
            }

            foreach (var attributeList in parameter.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (IsKeyedServiceAttribute(
                        attribute,
                        parameterSymbol,
                        context.SemanticModel,
                        context.Compilation,
                        context.CancellationToken))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Errors.DataLoaderKeyedServiceAttributeIgnored,
                            attribute.GetLocation()));
                    }
                }
            }
        }
    }

    private static bool IsKeyedServiceAttribute(
        AttributeSyntax attribute,
        IParameterSymbol parameter,
        SemanticModel semanticModel,
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        if (semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol is not IMethodSymbol attributeConstructor)
        {
            return false;
        }

        var attributeType = attributeConstructor.ContainingType;

        if (attributeType.ToDisplayString() == WellKnownAttributes.FromKeyedServicesAttribute)
        {
            return true;
        }

        if (!attributeType.IsOrInheritsFrom(WellKnownAttributes.ServiceAttribute))
        {
            return false;
        }

        if (attributeType.ToDisplayString() == WellKnownAttributes.ServiceAttribute)
        {
            return attributeConstructor.Parameters.Length == 1;
        }

        return parameter.GetServiceAttributeInfo(compilation).SourceDerivedServiceKey is not null;
    }
}
