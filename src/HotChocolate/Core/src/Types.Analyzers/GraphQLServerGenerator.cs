using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Filters;
using HotChocolate.Types.Analyzers.Generators;
using HotChocolate.Types.Analyzers.Inspectors;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers;

[Generator]
public class GraphQLServerGenerator : IIncrementalGenerator
{
    private static readonly ISyntaxInspector[] _inspectors =
    [
        new TypeAttributeInspector(),
        new ClassBaseClassInspector(),
        new ModuleInspector(),
        new DataLoaderInspector(),
        new DataLoaderDefaultsInspector(),
        new DataLoaderModuleInspector(),
        new OperationInspector(),
        new ObjectTypeExtensionInfoInspector(),
        new InterfaceTypeInfoInspector(),
        new RequestMiddlewareInspector()
    ];

    private static readonly ISyntaxGenerator[] _generators =
    [
        new TypeModuleSyntaxGenerator(),
        new TypesSyntaxGenerator(),
        new MiddlewareGenerator(),
        new DataLoaderModuleGenerator(),
        new DataLoaderGenerator()
    ];

    private static readonly Func<SyntaxNode, bool> _predicate;

    static GraphQLServerGenerator()
    {
        var filterBuilder = new SyntaxFilterBuilder();

        foreach (var inspector in _inspectors)
        {
            filterBuilder.AddRange(inspector.Filters);
        }

        _predicate = filterBuilder.Build();
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxInfos =
            context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => Predicate(s),
                    transform: static (ctx, _) => Transform(ctx))
                .WhereNotNull()
                .WithComparer(SyntaxInfoComparer.Default)
                .Collect();

        var valueProvider = context.CompilationProvider.Combine(syntaxInfos);

        context.RegisterSourceOutput(
            valueProvider,
            static (context, source) => Execute(context, source.Left, source.Right));
    }

    private static bool Predicate(SyntaxNode node)
        => _predicate(node);

    private static SyntaxInfo? Transform(GeneratorSyntaxContext context)
    {
        for (var i = 0; i < _inspectors.Length; i++)
        {
            if (_inspectors[i].TryHandle(context, out var syntaxInfo))
            {
                return syntaxInfo;
            }
        }

        return null;
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<SyntaxInfo> syntaxInfos)
    {
        foreach (var syntaxInfo in syntaxInfos.AsSpan())
        {
            if (syntaxInfo.Diagnostics.Length > 0)
            {
                foreach (var diagnostic in syntaxInfo.Diagnostics.AsSpan())
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        foreach (var generator in _generators.AsSpan())
        {
            generator.Generate(context, compilation, syntaxInfos);
        }
    }
}

file static class Extensions
{
    public static IncrementalValuesProvider<SyntaxInfo> WhereNotNull(
        this IncrementalValuesProvider<SyntaxInfo?> source)
        => source.Where(static t => t is not null)!;
}
