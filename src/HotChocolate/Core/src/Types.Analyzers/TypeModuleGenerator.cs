using System.Buffers;
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
    [
        new TypeAttributeInspector(),
        new ClassBaseClassInspector(),
        new ModuleInspector(),
        new DataLoaderInspector(),
        new DataLoaderDefaultsInspector(),
    ];

    private static readonly ISyntaxGenerator[] _generators =
    [
        new ModuleGenerator(),
        new DataLoaderGenerator(),
    ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(c => PostInitialization(c));

        var modulesAndTypes =
            context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsRelevant(s),
                    transform: TryGetModuleOrType)
                .Where(static t => t is not null)!
                .WithComparer(SyntaxInfoComparer.Default);

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
        => node is ClassDeclarationSyntax { BaseList.Types.Count: > 0, };

    private static bool IsTypeWithAttribute(SyntaxNode node)
        => node is BaseTypeDeclarationSyntax { AttributeLists.Count: > 0, };

    private static bool IsMethodWithAttribute(SyntaxNode node)
        => node is MethodDeclarationSyntax { AttributeLists.Count: > 0, };

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
        if (syntaxInfos.IsEmpty)
        {
            return;
        }

        var buffer = ArrayPool<ISyntaxInfo>.Shared.Rent(syntaxInfos.Length * 2);

        // prepare context
        for (var i = syntaxInfos.Length - 1; i >= 0; i--)
        {
            buffer[i] = syntaxInfos[i];
        }

        var nodes = buffer.AsSpan().Slice(0, syntaxInfos.Length);
        var batch = buffer.AsSpan().Slice(syntaxInfos.Length, syntaxInfos.Length);

        foreach (var generator in _generators)
        {
            var next = 0;

            // gather infos for current generator
            foreach (var node in nodes)
            {
                if (generator.Consume(node))
                {
                    batch[next++] = node;
                }
            }

            // generate
            if (next > 0)
            {
                generator.Generate(context, compilation, batch.Slice(0, next));
            }
        }

        ArrayPool<ISyntaxInfo>.Shared.Return(buffer);
    }
}
