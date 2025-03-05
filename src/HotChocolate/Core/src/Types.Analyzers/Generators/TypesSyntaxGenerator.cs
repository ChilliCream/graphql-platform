using System.Buffers.Text;
using System.Collections.Immutable;
using System.Security.Cryptography;
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
        string assemblyName,
        ImmutableArray<SyntaxInfo> syntaxInfos)
    {
        if (syntaxInfos.IsEmpty)
        {
            return;
        }

        var module = syntaxInfos.GetModuleInfo(assemblyName, out _);

        // the generator is disabled.
        if(module.Options == ModuleOptions.Disabled)
        {
            return;
        }

        var sb = PooledObjects.GetStringBuilder();

        WriteTypes(context, syntaxInfos, sb);
        WriteTypes2(context, syntaxInfos, sb);

        sb.Clear();

        WriteResolvers(context, syntaxInfos, sb);

        PooledObjects.Return(sb);
    }

    private static void WriteTypes2(
        SourceProductionContext context,
        ImmutableArray<SyntaxInfo> syntaxInfos,
        StringBuilder sb)
    {
        var typeLookup = new DefaultLocalTypeLookup(syntaxInfos);

        foreach (var type in syntaxInfos.OfType<IOutputTypeInfo>())
        {
            sb.Clear();

            if (type is ObjectTypeExtensionInfo objectType)
            {
                var file = new ObjectTypeFileBuilder(sb);
                file.WriteHeader();
                file.WriteBeginNamespace(objectType);
                file.WriteBeginClass(objectType);
                file.WriteInitializeMethod(objectType);
                file.WriteConfigureMethod(objectType);
                file.WriteBeginResolverClass();
                file.WriteResolverFields(objectType);
                file.WriteResolverConstructor(objectType, typeLookup);
                file.WriteResolverMethods(objectType, typeLookup);
                file.WriteEndResolverClass();
                file.WriteEndClass();
                file.WriteEndNamespace();
                file.Flush();

                context.AddSource(CreateFileName(objectType), sb.ToString());
            }
        }

        static string CreateFileName(IOutputTypeInfo type)
        {
            Span<byte> hash = stackalloc byte[64];
            var bytes = Encoding.UTF8.GetBytes(type.Namespace);
            MD5.HashData(bytes, hash);
            Base64.EncodeToUtf8InPlace(hash, 16, out var written);
            hash = hash[..written];

            for (var i = 0; i < hash.Length; i++)
            {
                if (hash[i] == (byte)'+')
                {
                    hash[i] = (byte)'-';
                }
                else if (hash[i] == (byte)'/')
                {
                    hash[i] = (byte)'_';
                }
                else if(hash[i] == (byte)'=')
                {
                    hash = hash[..i];
                    break;
                }
            }

            return $"{type.Name}.{Encoding.UTF8.GetString(hash)}.hc.g.cs";
        }
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
            .GroupBy(t => t.Namespace))
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
                if (typeInfo.Diagnostics.Length > 0 || typeInfo is ObjectTypeExtensionInfo)
                {
                    continue;
                }

                var classGenerator = typeInfo is InterfaceTypeExtensionInfo
                    ? new InterfaceTypeExtensionFileBuilder(sb, group.Key)
                    : (IOutputTypeFileBuilder)new ObjectTypeExtensionFileBuilder(sb, group.Key);

                if (!firstClass)
                {
                    sb.AppendLine();
                }

                firstClass = false;

                classGenerator.WriteBeginClass(typeInfo.Name);
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
            .GroupBy(t => t.Namespace))
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

                generator.WriteBeginClass(typeInfo.Name + "Resolvers");

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
