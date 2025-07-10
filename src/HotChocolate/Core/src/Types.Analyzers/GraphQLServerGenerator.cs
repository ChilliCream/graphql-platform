using System.Collections.Frozen;
using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Filters;
using HotChocolate.Types.Analyzers.Generators;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Inspectors;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace HotChocolate.Types.Analyzers;

[Generator]
public class GraphQLServerGenerator : IIncrementalGenerator
{
    private static readonly ISyntaxInspector[] s_allInspectors =
    [
        new TypeAttributeInspector(),
        new ClassBaseClassInspector(),
        new ModuleInspector(),
        new DataLoaderInspector(),
        new DataLoaderDefaultsInspector(),
        new DataLoaderModuleInspector(),
        new OperationInspector(),
        new ObjectTypeInspector(),
        new InterfaceTypeInfoInspector(),
        new RequestMiddlewareInspector(),
        new ConnectionInspector()
    ];

    private static readonly IPostCollectSyntaxTransformer[] s_postCollectTransformers =
    [
        new ConnectionTypeTransformer()
    ];

    private static readonly ISyntaxGenerator[] s_generators =
    [
        new TypeModuleSyntaxGenerator(),
        new TypesSyntaxGenerator(),
        new MiddlewareGenerator(),
        new DataLoaderModuleGenerator(),
        new DataLoaderGenerator()
    ];

    private static readonly FrozenDictionary<SyntaxKind, ImmutableArray<ISyntaxInspector>> s_inspectorLookup;
    private static readonly Func<SyntaxNode, bool> s_predicate;

    static GraphQLServerGenerator()
    {
        var filterBuilder = new SyntaxFilterBuilder();
        var inspectorLookup = new Dictionary<SyntaxKind, List<ISyntaxInspector>>();

        foreach (var inspector in s_allInspectors)
        {
            filterBuilder.AddRange(inspector.Filters);

            foreach (var supportedKind in inspector.SupportedKinds)
            {
                if (!inspectorLookup.TryGetValue(supportedKind, out var inspectors))
                {
                    inspectors = [];
                    inspectorLookup[supportedKind] = inspectors;
                }

                inspectors.Add(inspector);
            }
        }

        s_predicate = filterBuilder.Build();
        s_inspectorLookup = inspectorLookup.ToFrozenDictionary(t => t.Key, t => t.Value.ToImmutableArray());
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
        foreach (var transformer in s_postCollectTransformers)
        {
            syntaxInfos = transformer.Transform(compilation, syntaxInfos);
        }

        return syntaxInfos;
    }

    private static bool Predicate(SyntaxNode node)
        => s_predicate(node);

    private static SyntaxInfo? Transform(GeneratorSyntaxContext context)
    {
        if (!s_inspectorLookup.TryGetValue(context.Node.Kind(), out var inspectors))
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
        var processedFiles = PooledObjects.GetStringSet();

        try
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

            foreach (var generator in s_generators.AsSpan())
            {
                generator.Generate(context, assemblyName, syntaxInfos, AddSource);
            }
        }
        finally
        {
            PooledObjects.Return(processedFiles);
        }

        void AddSource(string fileName, SourceText sourceText)
        {
            if (processedFiles.Add(fileName))
            {
                context.AddSource(fileName, sourceText);
            }
        }
    }
}

file static class Extensions
{
    public static IncrementalValuesProvider<SyntaxInfo> WhereNotNull(
        this IncrementalValuesProvider<SyntaxInfo?> source)
        => source.Where(static t => t is not null)!;
}
