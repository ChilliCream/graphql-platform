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
    };

    private static readonly ISyntaxGenerator[] _generators =
    {
        new ModuleGenerator(),
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
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

    private static bool IsRelevant(SyntaxNode node)
        => IsTypeWithAttribute(node) ||
            IsClassWithBaseClass(node) ||
            IsAssemblyAttributeList(node);

    private static bool IsClassWithBaseClass(SyntaxNode node)
        => node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 };

    private static bool IsTypeWithAttribute(SyntaxNode node)
        => node is BaseTypeDeclarationSyntax { AttributeLists.Count: > 0 };

    private static bool IsAssemblyAttributeList(SyntaxNode node)
        => node is AttributeListSyntax;

    private static ISyntaxInfo? TryGetModuleOrType(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < _inspectors.Length; i++)
        {
            if (_inspectors[i].TryHandle(context, out ISyntaxInfo? syntaxInfo))
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
        var current = all;
        var batch = new HashSet<ISyntaxInfo>();

        // unpack aggregates
        for (var i = 0; i < current.Length; i++)
        {
            var syntaxInfo = current[i];

            if (syntaxInfo is AggregateInfo aggregate)
            {
                all = all.Remove(aggregate);
                all = all.AddRange(aggregate.Items);
            }
        }

        foreach (ISyntaxGenerator syntaxGenerator in _generators)
        {
            // capture the current list of infos
            current = all;

            // gather infos for current generator
            for (var i = 0; i < current.Length; i++)
            {
                var syntaxInfo = current[i];

                if (syntaxGenerator.Consume(syntaxInfo))
                {
                    all = all.Remove(syntaxInfo);
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
