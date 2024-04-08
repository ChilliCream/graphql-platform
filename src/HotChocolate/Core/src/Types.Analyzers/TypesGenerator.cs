using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Generators;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Generator]
public class TypesGenerator : IIncrementalGenerator
{
    private static readonly ISyntaxInspector[] _inspectors =
    [
        new ObjectTypeExtensionInfoInspector(),
    ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
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

    private static bool IsRelevant(SyntaxNode node)
        => IsClassWithAttribute(node) || IsAssemblyAttributeList(node);

    private static bool IsClassWithAttribute(SyntaxNode node)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0, };

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

        var sb = StringBuilderPool.Get();
        var first = true;

        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is not ObjectTypeExtensionInfo objectTypeExtension)
            {
                continue;
            }

            if (objectTypeExtension.Diagnostics.Length > 0)
            {
                foreach (var diagnostic in objectTypeExtension.Diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
                continue;
            }

            var generator = new ObjectTypeExtensionSyntaxGenerator(
                sb,
                objectTypeExtension.Type.ContainingNamespace.ToDisplayString());

            if (first)
            {
                generator.WriterHeader();
                first = false;
            }

            generator.WriteBeginNamespace();
            generator.WriteBeginClass(objectTypeExtension.Type.Name);
            generator.WriteInitializeMethod(objectTypeExtension);
            generator.WriteConfigureMethod(objectTypeExtension);
            generator.WriteEndClass();
            generator.WriteEndNamespace();
        }

        context.AddSource(WellKnownFileNames.TypesFile, sb.ToString());
        StringBuilderPool.Return(sb);
    }
}
