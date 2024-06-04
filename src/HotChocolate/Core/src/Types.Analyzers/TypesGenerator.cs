using System.Collections.Immutable;
using System.Text;
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

        WriteTypes(context, syntaxInfos, sb);

        sb.Clear();

        WriteResolvers(context, compilation, syntaxInfos, sb);

        StringBuilderPool.Return(sb);
    }

    private static void WriteTypes(
        SourceProductionContext context,
        ImmutableArray<ISyntaxInfo> syntaxInfos,
        StringBuilder sb)
    {
        var firstNamespace = true;
        foreach (var group in syntaxInfos
            .OfType<ObjectTypeExtensionInfo>()
            .GroupBy(t => t.Type.ContainingNamespace.ToDisplayString()))
        {
            var generator = new ObjectTypeExtensionSyntaxGenerator(sb, group.Key);

            if (firstNamespace)
            {
                generator.WriteHeader();
                firstNamespace = false;
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
    }

    private static void WriteResolvers(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ISyntaxInfo> syntaxInfos,
        StringBuilder sb)
    {
        var generator = new ResolverSyntaxGenerator(sb);
        generator.WriteHeader();

        var firstNamespace = true;
        foreach (var group in syntaxInfos
            .OfType<ObjectTypeExtensionInfo>()
            .GroupBy(t => t.Type.ContainingNamespace.ToDisplayString()))
        {
            if(!firstNamespace)
            {
                sb.AppendLine();
            }
            firstNamespace = false;

            generator.WriteBeginNamespace(group.Key);

            var firstClass = true;
            foreach (var objectTypeExtension in group)
            {
                if(!firstClass)
                {
                    sb.AppendLine();
                }
                firstClass = false;

                var resolverInfos = objectTypeExtension.Members.Select(
                    m => CreateResolverInfo(objectTypeExtension, m));

                generator.WriteBeginClass(objectTypeExtension.Type.Name + "Resolvers");

                if (generator.AddResolverDeclarations(resolverInfos))
                {
                    sb.AppendLine();
                }

                generator.AddParameterInitializer(resolverInfos);

                foreach (var member in objectTypeExtension.Members)
                {
                    sb.AppendLine();
                    generator.AddResolver(
                        new ResolverName(objectTypeExtension.Type.Name, member.Name),
                        member,
                        compilation);
                }

                generator.WriteEndClass();
            }

            generator.WriteEndNamespace();
        }

        context.AddSource(WellKnownFileNames.ResolversFile, sb.ToString());
    }

    private static ResolverInfo CreateResolverInfo(
        ObjectTypeExtensionInfo objectTypeExtension,
        ISymbol member)
        => new ResolverInfo(
            new ResolverName(objectTypeExtension.Type.Name, member.Name),
            member as IMethodSymbol);
}
