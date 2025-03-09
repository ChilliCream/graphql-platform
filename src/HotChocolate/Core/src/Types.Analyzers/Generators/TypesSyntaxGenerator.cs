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

        sb.Clear();
        PooledObjects.Return(sb);
    }

    private static void WriteTypes(
        SourceProductionContext context,
        ImmutableArray<SyntaxInfo> syntaxInfos,
        StringBuilder sb)
    {
        var typeLookup = new DefaultLocalTypeLookup(syntaxInfos);

        foreach (var type in syntaxInfos.OrderBy(t => t.OrderByKey).OfType<IOutputTypeInfo>())
        {
            sb.Clear();

            if (type is ObjectTypeInfo objectType)
            {
                var file = new ObjectTypeFileBuilder(sb);
                WriteFile(file, objectType, typeLookup);
                context.AddSource(CreateFileName(objectType), sb.ToString());
            }

            if(type is InterfaceTypeInfo interfaceType)
            {
                var file = new InterfaceTypeFileBuilder(sb);
                WriteFile(file, interfaceType, typeLookup);
                context.AddSource(CreateFileName(interfaceType), sb.ToString());
            }

            if(type is RootTypeInfo rootType)
            {
                var file = new RootTypeFileBuilder(sb);
                WriteFile(file, rootType, typeLookup);
                context.AddSource(CreateFileName(rootType), sb.ToString());
            }

            if(type is ConnectionTypeInfo connectionType)
            {
                var file = new ConnectionTypeFileBuilder(sb);
                WriteFile(file, connectionType, typeLookup);
                context.AddSource(CreateFileName(connectionType), sb.ToString());
            }

            if(type is EdgeTypeInfo edgeType)
            {
                var file = new EdgeTypeFileBuilder(sb);
                WriteFile(file, edgeType, typeLookup);
                context.AddSource(CreateFileName(edgeType), sb.ToString());
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

    private static void WriteFile(TypeFileBuilderBase file, IOutputTypeInfo type, ILocalTypeLookup typeLookup)
    {
        file.WriteHeader();
        file.WriteBeginNamespace(type);
        file.WriteBeginClass(type);
        file.WriteInitializeMethod(type);
        file.WriteConfigureMethod(type);
        file.WriteBeginResolverClass();
        file.WriteResolverFields(type);
        file.WriteResolverConstructor(type, typeLookup);
        file.WriteResolverMethods(type, typeLookup);
        file.WriteEndResolverClass();
        file.WriteEndClass();
        file.WriteEndNamespace();
        file.Flush();
    }
}
