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
    {
        if (syntaxInfos.IsEmpty)
        {
            return;
        }

        var module = syntaxInfos.GetModuleInfo(compilation.AssemblyName, out _);

        // the generator is disabled.
        if(module.Options == ModuleOptions.Disabled)
        {
            return;
        }

        var sb = PooledObjects.GetStringBuilder();

        WriteTypes(context, syntaxInfos, sb);

        sb.Clear();

        WriteResolvers(context, syntaxInfos, sb);

        PooledObjects.Return(sb);
    }

    private static void WriteTypes(
        SourceProductionContext context,
        ImmutableArray<SyntaxInfo> syntaxInfos,
        StringBuilder sb)
    {
        var hasTypes = false;
        var firstNamespace = true;
        foreach (var group in syntaxInfos
            .OfType<IOutputTypeInfo>()
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
            foreach (var typeInfo in group)
            {
                if (typeInfo.Diagnostics.Length > 0)
                {
                    continue;
                }

                var classGenerator = typeInfo is ObjectTypeExtensionInfo
                    ? (IOutputTypeFileBuilder)new ObjectTypeExtensionFileBuilder(sb, group.Key)
                    : new InterfaceTypeExtensionFileBuilder(sb, group.Key);

                if (!firstClass)
                {
                    sb.AppendLine();
                }

                firstClass = false;

                classGenerator.WriteBeginClass(typeInfo.Type.Name);
                classGenerator.WriteInitializeMethod(typeInfo);
                sb.AppendLine();
                classGenerator.WriteConfigureMethod(typeInfo);
                classGenerator.WriteEndClass();
                hasTypes = true;

            }

            generator.WriteEndNamespace();
        }

        if (hasTypes)
        {
            context.AddSource(WellKnownFileNames.TypesFile, sb.ToString());
        }
    }

    private static void WriteResolvers(
        SourceProductionContext context,
        ImmutableArray<SyntaxInfo> syntaxInfos,
        StringBuilder sb)
    {
        var hasResolvers = false;
        var typeLookup = new DefaultLocalTypeLookup(syntaxInfos);

        var generator = new ResolverFileBuilder(sb);
        generator.WriteHeader();

        var firstNamespace = true;
        foreach (var group in syntaxInfos
            .OfType<IOutputTypeInfo>()
            .GroupBy(t => t.Type.ContainingNamespace.ToDisplayString()))
        {
            if (!firstNamespace)
            {
                sb.AppendLine();
            }

            firstNamespace = false;

            generator.WriteBeginNamespace(group.Key);

            var firstClass = true;
            foreach (var typeInfo in group)
            {
                if (!firstClass)
                {
                    sb.AppendLine();
                }

                firstClass = false;

                var resolvers = typeInfo.Resolvers;

                if (typeInfo is ObjectTypeExtensionInfo { NodeResolver: { } nodeResolver })
                {
                    resolvers = resolvers.Add(nodeResolver);
                }

                generator.WriteBeginClass(typeInfo.Type.Name + "Resolvers");

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
                hasResolvers = true;
            }

            generator.WriteEndNamespace();
        }

        if (hasResolvers)
        {
            context.AddSource(WellKnownFileNames.ResolversFile, sb.ToString());
        }
    }
}
