using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DataLoaderPublicInterfaceAccessModifierAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.DataLoaderPublicInterfaceAccessModifier];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var methodSymbol = (IMethodSymbol)context.Symbol;
        var attributes = GenericDataLoaderAnalyzerHelper.GetGenericDataLoaderAttributes(
            methodSymbol,
            context.Compilation);

        if (attributes.Length != 1
            || !GenericDataLoaderAnalyzerHelper.HasExactlyOneDataLoaderAttribute(
                methodSymbol,
                context.Compilation))
        {
            return;
        }

        var attribute = attributes[0];

        if (!HasPublicInterfaceAccessModifier(attribute)
            || attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken)
                is not AttributeSyntax attributeSyntax)
        {
            return;
        }

        var location = attributeSyntax.ArgumentList?.Arguments.FirstOrDefault(t =>
                t.NameEquals?.Name.Identifier.ValueText == "AccessModifier")
            ?.Expression.GetLocation()
            ?? attributeSyntax.GetLocation();

        context.ReportDiagnostic(Diagnostic.Create(
            Errors.DataLoaderPublicInterfaceAccessModifier,
            location));
    }

    private static bool HasPublicInterfaceAccessModifier(AttributeData attribute)
        => attribute.NamedArguments.FirstOrDefault(t => t.Key == "AccessModifier").Value.Value is int value
            && value == 2;
}
