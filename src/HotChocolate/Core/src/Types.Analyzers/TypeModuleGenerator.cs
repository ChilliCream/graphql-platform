using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Filters;
using HotChocolate.Types.Analyzers.Generators;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers;

[Generator]
public class TypeModuleGenerator : IIncrementalGenerator
{
    private static readonly ISyntaxInspector[] _inspectors =
    [
        new TypeAttributeInspector(),
        new ClassBaseClassInspector(),
        new ModuleInspector(),
        new DataLoaderInspector(),
        new DataLoaderDefaultsInspector(),
        new OperationInspector(),
        new ObjectTypeExtensionInfoInspector(),
        new ObjectTypeExtensionInfoInspector(),
    ];

    private static readonly ISyntaxGenerator[] _generators =
    [
        new TypeModuleSyntaxGenerator(),
        new TypesSyntaxGenerator(),
    ];

    private static readonly Func<SyntaxNode, bool> _predicate;

    static TypeModuleGenerator()
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

    private static ISyntaxInfo? Transform(GeneratorSyntaxContext context)
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
        ImmutableArray<ISyntaxInfo> syntaxInfos)
    {
        for (var i = 0; i < _generators.Length; i++)
        {
            _generators[i].Generate(context, compilation, syntaxInfos);
        }
    }
}

file static class Extensions
{
    public static IncrementalValuesProvider<ISyntaxInfo> WhereNotNull(
        this IncrementalValuesProvider<ISyntaxInfo?> source)
        => source.Where(static t => t is not null)!;
}
