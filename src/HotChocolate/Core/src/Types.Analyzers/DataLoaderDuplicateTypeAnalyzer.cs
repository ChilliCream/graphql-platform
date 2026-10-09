using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DataLoaderDuplicateTypeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.DataLoaderDuplicateType];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static context =>
        {
            var dataLoaders = new ConcurrentQueue<GenericDataLoaderMethod>();

            context.RegisterSymbolAction(
                context => AnalyzeMethod(context, dataLoaders),
                SymbolKind.Method);
            context.RegisterCompilationEndAction(
                context => ReportDuplicates(context, dataLoaders));
        });
    }

    private static void AnalyzeMethod(
        SymbolAnalysisContext context,
        ConcurrentQueue<GenericDataLoaderMethod> dataLoaders)
    {
        var methodSymbol = (IMethodSymbol)context.Symbol;
        var attributes = GenericDataLoaderAnalyzerHelper.GetGenericDataLoaderAttributes(
            methodSymbol,
            context.Compilation);

        if (attributes.Length != 1
            || !GenericDataLoaderAnalyzerHelper.HasExactlyOneDataLoaderAttribute(
                methodSymbol,
                context.Compilation)
            || !GenericDataLoaderAnalyzerHelper.IsValidGenericDataLoaderMethod(
                methodSymbol,
                attributes[0],
                context.Compilation)
            || attributes[0].AttributeClass?.TypeArguments[0] is not { } dataLoaderType)
        {
            return;
        }

        dataLoaders.Enqueue(new GenericDataLoaderMethod(
            dataLoaderType,
            GetLocation(attributes[0], methodSymbol, context.CancellationToken)));
    }

    private static void ReportDuplicates(
        CompilationAnalysisContext context,
        ConcurrentQueue<GenericDataLoaderMethod> dataLoaders)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var groups = new Dictionary<ITypeSymbol, List<GenericDataLoaderMethod>>(
            SymbolEqualityComparer.Default);

        foreach (var dataLoader in dataLoaders)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (!groups.TryGetValue(dataLoader.Type, out var methods))
            {
                methods = [];
                groups.Add(dataLoader.Type, methods);
            }

            methods.Add(dataLoader);
        }

        var dataLoaderGroups = groups.ToArray();
        context.CancellationToken.ThrowIfCancellationRequested();
        Array.Sort(
            dataLoaderGroups,
            static (left, right) => StringComparer.Ordinal.Compare(
                left.Key.ToDisplayString(),
                right.Key.ToDisplayString()));

        foreach (var dataLoaderGroup in dataLoaderGroups)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var methods = dataLoaderGroup.Value.ToArray();
            Array.Sort(
                methods,
                static (left, right) =>
                {
                    var pathComparison = StringComparer.Ordinal.Compare(
                        left.Location.SourceTree?.FilePath,
                        right.Location.SourceTree?.FilePath);
                    return pathComparison != 0
                        ? pathComparison
                        : left.Location.SourceSpan.Start.CompareTo(right.Location.SourceSpan.Start);
                });
            context.CancellationToken.ThrowIfCancellationRequested();

            if (methods.Length < 2)
            {
                continue;
            }

            var dataLoaderType = dataLoaderGroup.Key.ToDisplayString();

            foreach (var method in methods)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                context.ReportDiagnostic(Diagnostic.Create(
                    Errors.DataLoaderDuplicateType,
                    method.Location,
                    dataLoaderType));
            }
        }
    }

    private static Location GetLocation(
        AttributeData attribute,
        IMethodSymbol methodSymbol,
        CancellationToken cancellationToken)
        => attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation()
            ?? methodSymbol.Locations.FirstOrDefault()
            ?? Location.None;

    private sealed record GenericDataLoaderMethod(ITypeSymbol Type, Location Location);
}
