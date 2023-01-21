using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Generators;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Generator]
public class TypeModuleGenerator : IIncrementalGenerator
{
    private static readonly ISyntaxInspector[] _inspectors =
    {
        new TypeAttributeInspector(),
        new ClassBaseClassInspector(),
        new ModuleInspector(),
        new DataLoaderInspector(),
        new DataLoaderDefaultsInspector()
    };

    private static readonly ISyntaxGenerator[] _generators =
    {
        new ModuleGenerator(),
        new DataLoaderGenerator()
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(c => PostInitialization(c));

        IncrementalValuesProvider<ISyntaxInfo> modulesAndTypes =
            context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsRelevant(s),
                    transform: TryGetModuleOrType)
                .Where(static t => t is not null)!;

        var valueProvider = context.CompilationProvider.Combine(modulesAndTypes.Collect());

        context.RegisterSourceOutput(
            valueProvider,
            static (context, source) => Execute(context, source.Left, source.Right));
    }

    private static void PostInitialization(IncrementalGeneratorPostInitializationContext context)
    {
        foreach (var syntaxGenerator in _generators)
        {
            syntaxGenerator.Initialize(context);
        }
    }

    private static bool IsRelevant(SyntaxNode node)
        => IsTypeWithAttribute(node) ||
            IsClassWithBaseClass(node) ||
            IsAssemblyAttributeList(node) ||
            IsMethodWithAttribute(node);

    private static bool IsClassWithBaseClass(SyntaxNode node)
        => node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 };

    private static bool IsTypeWithAttribute(SyntaxNode node)
        => node is BaseTypeDeclarationSyntax { AttributeLists.Count: > 0 };

    private static bool IsMethodWithAttribute(SyntaxNode node)
        => node is MethodDeclarationSyntax { AttributeLists.Count: > 0 };

    private static bool IsAssemblyAttributeList(SyntaxNode node)
        => node is AttributeListSyntax;

    private static ISyntaxInfo? TryGetModuleOrType(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
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
        var all = syntaxInfos;
        var batch = new HashSet<ISyntaxInfo>();

        // unpack aggregates
        for (var i = 0; i < all.Length; i++)
        {
            var syntaxInfo = all[i];

            if (syntaxInfo is AggregateInfo aggregate)
            {
                all = all.Remove(aggregate);
                all = all.AddRange(aggregate.Items);
            }
        }

        foreach (var syntaxGenerator in _generators)
        {
            // gather infos for current generator
            for (var i = all.Length - 1; i >= 0; i--)
            {
                var syntaxInfo = all[i];

                if (syntaxGenerator.Consume(syntaxInfo))
                {
                    batch.Add(syntaxInfo);
                }
            }

            // generate
            if (batch.Count > 0)
            {
                syntaxGenerator.Generate(context, compilation, batch);
            }

            // reset context
            batch.Clear();

            if (all.IsEmpty)
            {
                break;
            }
        }
    }
}
