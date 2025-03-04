using System.Collections.Frozen;
using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Filters;
using HotChocolate.Types.Analyzers.Generators;
using HotChocolate.Types.Analyzers.Inspectors;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HotChocolate.Types.Analyzers;

#pragma warning disable RS1041
[Generator]
#pragma warning restore RS1041
public class GraphQLServerGenerator : IIncrementalGenerator
{
    private static readonly ISyntaxInspector[] _allInspectors =
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
        new RequestMiddlewareInspector(),
        new ConnectionInspector()
    ];

    private static readonly IPostCollectSyntaxTransformer[] _postCollectTransformers =
    [
        new ConnectionTypeTransformer()
    ];

    private static readonly ISyntaxGenerator[] _generators =
    [
        new TypeModuleSyntaxGenerator(),
        new TypesSyntaxGenerator(),
        new MiddlewareGenerator(),
        new DataLoaderModuleGenerator(),
        new DataLoaderGenerator()
    ];

    private static readonly FrozenDictionary<SyntaxKind, ImmutableArray<ISyntaxInspector>> _inspectorLookup;
    private static readonly Func<SyntaxNode, bool> _predicate;

    static GraphQLServerGenerator()
    {
        var filterBuilder = new SyntaxFilterBuilder();
        var inspectorLookup = new Dictionary<SyntaxKind, List<ISyntaxInspector>>();

        foreach (var inspector in _allInspectors)
        {
            filterBuilder.AddRange(inspector.Filters);

            foreach (var supportedKind in inspector.SupportedKinds)
            {
                if(!inspectorLookup.TryGetValue(supportedKind, out var inspectors))
                {
                    inspectors = [];
                    inspectorLookup[supportedKind] = inspectors;
                }
                inspectors.Add(inspector);
            }
        }

        _predicate = filterBuilder.Build();
        _inspectorLookup = inspectorLookup.ToFrozenDictionary(t => t.Key, t => t.Value.ToImmutableArray());
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

        var postProcessedSyntaxInfos =
            context.CompilationProvider
                .Combine(syntaxInfos)
                .Select((ctx, _) => OnAfterCollect(ctx.Left, ctx.Right));

        var assemblyNameProvider = context.CompilationProvider
            .Select(static (c, _) => c.AssemblyName!);

        var valueProvider = assemblyNameProvider.Combine(postProcessedSyntaxInfos);

        context.RegisterSourceOutput(
            valueProvider,
            static (context, source) => Execute(context, source.Left, source.Right));
    }

    private static ImmutableArray<SyntaxInfo> OnAfterCollect(
        Compilation compilation,
        ImmutableArray<SyntaxInfo> syntaxInfos)
    {
        foreach (var transformer in _postCollectTransformers)
        {
            syntaxInfos = transformer.Transform(compilation, syntaxInfos);
        }

        return syntaxInfos;
    }

    private static bool Predicate(SyntaxNode node)
        => _predicate(node);

    private static SyntaxInfo? Transform(GeneratorSyntaxContext context)
    {
        if (!_inspectorLookup.TryGetValue(context.Node.Kind(), out var inspectors))
        {
            return null;
        }

        foreach (var inspector in inspectors)
        {
            if (inspector.TryHandle(context, out var syntaxInfo))
            {
                return syntaxInfo;
            }
        }

        return null;
    }

    private static void Execute(
        SourceProductionContext context,
        string assemblyName,
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
            generator.Generate(context, assemblyName, syntaxInfos);
        }
    }
}

file static class Extensions
{
    public static IncrementalValuesProvider<SyntaxInfo> WhereNotNull(
        this IncrementalValuesProvider<SyntaxInfo?> source)
        => source.Where(static t => t is not null)!;
}
