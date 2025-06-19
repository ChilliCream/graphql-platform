using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using HotChocolate.Types.Analyzers.FileBuilders;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace HotChocolate.Types.Analyzers.Generators;

public sealed class TypesSyntaxGenerator : ISyntaxGenerator
{
    public void Generate(
        SourceProductionContext context,
        string assemblyName,
        ImmutableArray<SyntaxInfo> syntaxInfos,
        Action<string, SourceText> addSource)
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

        WriteTypes(syntaxInfos, sb, addSource);

        sb.Clear();
        PooledObjects.Return(sb);
    }

    private static void WriteTypes(
        ImmutableArray<SyntaxInfo> syntaxInfos,
        StringBuilder sb,
        Action<string, SourceText> addSource)
    {
        var typeLookup = new DefaultLocalTypeLookup(syntaxInfos);

        foreach (var type in syntaxInfos.OrderBy(t => t.OrderByKey).OfType<IOutputTypeInfo>())
        {
            sb.Clear();

            if (type is ObjectTypeInfo objectType)
            {
                var file = new ObjectTypeFileBuilder(sb);
                WriteFile(file, objectType, typeLookup);
                addSource(CreateFileName(objectType), SourceText.From(sb.ToString(), Encoding.UTF8));
            }

            if(type is InterfaceTypeInfo interfaceType)
            {
                var file = new InterfaceTypeFileBuilder(sb);
                WriteFile(file, interfaceType, typeLookup);
                addSource(CreateFileName(interfaceType), SourceText.From(sb.ToString(), Encoding.UTF8));
            }

            if(type is RootTypeInfo rootType)
            {
                var file = new RootTypeFileBuilder(sb);
                WriteFile(file, rootType, typeLookup);
                addSource(CreateFileName(rootType), SourceText.From(sb.ToString(), Encoding.UTF8));
            }

            if(type is ConnectionTypeInfo connectionType)
            {
                var file = new ConnectionTypeFileBuilder(sb);
                WriteFile(file, connectionType, typeLookup);
                addSource(CreateFileName(connectionType), SourceText.From(sb.ToString(), Encoding.UTF8));
            }

            if(type is EdgeTypeInfo edgeType)
            {
                var file = new EdgeTypeFileBuilder(sb);
                WriteFile(file, edgeType, typeLookup);
                addSource(CreateFileName(edgeType), SourceText.From(sb.ToString(), Encoding.UTF8));
            }
        }

        static string CreateFileName(IOutputTypeInfo type)
        {
#if NET8_0_OR_GREATER
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
#else
            var bytes = Encoding.UTF8.GetBytes(type.Namespace);
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(bytes);
            var hashString = Convert.ToBase64String(hash, Base64FormattingOptions.None);
            hashString = hashString.Replace("+", "-").Replace("/", "_").TrimEnd('=');
            return $"{type.Name}.{hashString}.hc.g.cs";
#endif
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
