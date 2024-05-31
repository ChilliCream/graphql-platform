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

        foreach (var group in syntaxInfos
            .OfType<ObjectTypeExtensionInfo>()
            .GroupBy(t => t.Type.ContainingNamespace.ToDisplayString()))
        {
            var generator = new ObjectTypeExtensionSyntaxGenerator(sb, group.Key);

            if (first)
            {
                generator.WriterHeader();
                first = false;
            }

            generator.WriteBeginNamespace();

            var firstClass = true;

            foreach (var objectTypeExtension in group)
            {
                if (objectTypeExtension.Diagnostics.Length > 0)
                {
                    foreach (var diagnostic in objectTypeExtension.Diagnostics)
                    {
                        context.ReportDiagnostic(diagnostic);
                    }

                    continue;
                }

                if (!firstClass)
                {
                    sb.AppendLine();
                }
                firstClass = false;

                generator.WriteBeginClass(objectTypeExtension.Type.Name);
                generator.WriteInitializeMethod(objectTypeExtension);
                sb.AppendLine();
                generator.WriteConfigureMethod(objectTypeExtension);
                generator.WriteEndClass();
            }

            generator.WriteEndNamespace();
        }

        context.AddSource(WellKnownFileNames.TypesFile, sb.ToString());

        sb.Clear();
        var generator2 = new ResolverSyntaxGenerator(sb, "HotChocolate.Resolvers");

        generator2.WriterHeader();
        generator2.WriteBeginNamespace();
        generator2.WriteBeginClass("Abc");

        generator2.AddResolverDeclarations(
            syntaxInfos
                .OfType<ObjectTypeExtensionInfo>()
                .SelectMany(static t => t.Members.Select(m => CreateResolverName(t, m))));
        sb.AppendLine();

        var firstResolver = true;

        foreach (var objectTypeExtension in syntaxInfos.OfType<ObjectTypeExtensionInfo>())
        {
            foreach (var member in objectTypeExtension.Members)
            {
                if (!firstResolver)
                {
                    sb.AppendLine();
                }
                firstResolver = false;

                generator2.AddResolver(
                    new ResolverName(
                        objectTypeExtension.Type.Name,
                        member.Name,
                        GetArgumentsCount(member)),
                    member);
            }
        }

        generator2.WriteEndClass();
        generator2.WriteEndNamespace();

        context.AddSource(WellKnownFileNames.ResolversFile, sb.ToString());

        StringBuilderPool.Return(sb);
    }

    private static ResolverName CreateResolverName(
        ObjectTypeExtensionInfo objectTypeExtension,
        ISymbol member)
        => new ResolverName(objectTypeExtension.Type.Name, member.Name, GetArgumentsCount(member));

    private static int GetArgumentsCount(ISymbol symbol)
    {
        if (symbol is IMethodSymbol methodSymbol)
        {
            return methodSymbol.Parameters.Length;
        }

        return 0;
    }
}
