using System.Collections.Immutable;
using System.Text;
using HotChocolate.Types.Analyzers.FileBuilders;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Generators;

public sealed class TypesSyntaxGenerator : ISyntaxGenerator
{
    public void Generate(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<SyntaxInfo> syntaxInfos)
        => Execute(context, syntaxInfos);

    private static void Execute(
        SourceProductionContext context,
        ImmutableArray<SyntaxInfo> syntaxInfos)
    {
        if (syntaxInfos.IsEmpty)
        {
            return;
        }

        var sb = StringBuilderPool.Get();

        WriteTypes(context, syntaxInfos, sb);

        sb.Clear();

        WriteResolvers(context, syntaxInfos, sb);

        StringBuilderPool.Return(sb);
    }

    private static void WriteTypes(
        SourceProductionContext context,
        ImmutableArray<SyntaxInfo> syntaxInfos,
        StringBuilder sb)
    {
        var firstNamespace = true;
        foreach (var group in syntaxInfos
            .OfType<ObjectTypeExtensionInfo>()
            .GroupBy(t => t.Type.ContainingNamespace.ToDisplayString()))
        {
            var generator = new ObjectTypeExtensionFileBuilder(sb, group.Key);

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
        ImmutableArray<SyntaxInfo> syntaxInfos,
        StringBuilder sb)
    {
        var typeLookup = new DefaultLocalTypeLookup(syntaxInfos);

        var generator = new ResolverFileBuilder(sb);
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

                var resolvers = objectTypeExtension.Resolvers;

                if (objectTypeExtension.NodeResolver is not null)
                {
                    resolvers = resolvers.Add(objectTypeExtension.NodeResolver);
                }

                generator.WriteBeginClass(objectTypeExtension.Type.Name + "Resolvers");

                if (generator.AddResolverDeclarations(resolvers))
                {
                    sb.AppendLine();
                }

                generator.AddParameterInitializer(resolvers, typeLookup);

                foreach (var resolver in resolvers)
                {
                    sb.AppendLine();
                    generator.AddResolver(resolver, typeLookup);
                }

                generator.WriteEndClass();
            }

            generator.WriteEndNamespace();
        }

        context.AddSource(WellKnownFileNames.ResolversFile, sb.ToString());
    }
}
