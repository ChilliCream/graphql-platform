using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DataLoaderKeyedServiceOnConstructorParameterAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.DataLoaderKeyedServiceOnConstructorParameter];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static context =>
        {
            var dataLoaderType = context.Compilation.GetTypeByMetadataName(WellKnownTypes.DataLoader);

            if (dataLoaderType is not null)
            {
                context.RegisterSymbolAction(
                    c => AnalyzeNamedType(c, dataLoaderType),
                    SymbolKind.NamedType);
            }
        });
    }

    private static void AnalyzeNamedType(
        SymbolAnalysisContext context,
        INamedTypeSymbol dataLoaderType)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (!type.AllInterfaces.Any(t => SymbolEqualityComparer.Default.Equals(t, dataLoaderType)))
        {
            return;
        }

        foreach (var constructor in type.InstanceConstructors)
        {
            foreach (var parameter in constructor.Parameters)
            {
                foreach (var attribute in parameter.GetAttributes())
                {
                    if (!IsKeyedServiceAttribute(attribute, context.Compilation))
                    {
                        continue;
                    }

                    var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken)
                        .GetLocation()
                        ?? parameter.Locations.FirstOrDefault()
                        ?? Location.None;

                    context.ReportDiagnostic(Diagnostic.Create(
                        Errors.DataLoaderKeyedServiceOnConstructorParameter,
                        location));
                }
            }
        }
    }

    private static bool IsKeyedServiceAttribute(AttributeData attribute, Compilation compilation)
    {
        var attributeClass = attribute.AttributeClass;

        if (attributeClass is null
            || !attributeClass.IsOrInheritsFrom(WellKnownAttributes.ServiceAttribute))
        {
            return false;
        }

        if (attributeClass.ToDisplayString() == WellKnownAttributes.ServiceAttribute)
        {
            if (attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is not null)
            {
                return true;
            }

            return attribute.NamedArguments.Any(t => t is { Key: "Key", Value.Value: not null });
        }

        return attribute.GetDerivedServiceKey(compilation, out _)
            == SymbolExtensions.ServiceKeyExtractionResult.Key;
    }
}
