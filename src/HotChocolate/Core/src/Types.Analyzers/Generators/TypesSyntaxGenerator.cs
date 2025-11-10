#if NET8_0_OR_GREATER
using System.Buffers.Text;
#endif
using System.Collections.Concurrent;
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
    private static readonly MD5 s_md5 = MD5.Create();

    public void Generate(
        SourceProductionContext context,
        string assemblyName,
        ImmutableArray<SyntaxInfo> syntaxInfos,
        Action<string, string> addSource)
    {
        if (syntaxInfos.IsEmpty)
        {
            return;
        }

        var module = syntaxInfos.GetModuleInfo(assemblyName, out _);

        // the generator is disabled.
        if (module.Options == ModuleOptions.Disabled)
        {
            return;
        }

        WriteTypes(syntaxInfos, addSource);
    }

    private static void WriteTypes(
        ImmutableArray<SyntaxInfo> syntaxInfos,
        Action<string, string> addSource)
    {
        var typeLookup = new DefaultLocalTypeLookup(syntaxInfos);
        var namespaces = PooledObjects.GetStringDictionary();

        try
        {
            Parallel.ForEach(
                syntaxInfos.OrderBy(t => t.OrderByKey).OfType<IOutputTypeInfo>(),
                type =>
                {
                    var sb = PooledObjects.GetStringBuilder();

                    try
                    {
                        switch (type)
                        {
                            case ObjectTypeInfo objectType:
                            {
                                var file = new ObjectTypeFileBuilder(sb);
                                WriteFile(file, objectType, typeLookup);
                                addSource(
                                    CreateFileName(namespaces, objectType),
                                    sb.ToString());
                                break;
                            }
                            case InterfaceTypeInfo interfaceType:
                            {
                                var file = new InterfaceTypeFileBuilder(sb);
                                WriteFile(file, interfaceType, typeLookup);
                                addSource(
                                    CreateFileName(namespaces, interfaceType),
                                    sb.ToString());
                                break;
                            }
                            case RootTypeInfo rootType:
                            {
                                var file = new RootTypeFileBuilder(sb);
                                WriteFile(file, rootType, typeLookup);
                                addSource(
                                    CreateFileName(namespaces, rootType),
                                    sb.ToString());
                                break;
                            }
                            case ConnectionTypeInfo connectionType:
                            {
                                var file = new ConnectionTypeFileBuilder(sb);
                                WriteFile(file, connectionType, typeLookup);
                                addSource(
                                    CreateFileName(namespaces, connectionType),
                                    sb.ToString());
                                break;
                            }
                            case EdgeTypeInfo edgeType:
                            {
                                var file = new EdgeTypeFileBuilder(sb);
                                WriteFile(file, edgeType, typeLookup);
                                addSource(
                                    CreateFileName(namespaces, edgeType),
                                    sb.ToString());
                                break;
                            }
                        }
                    }
                    finally
                    {
                        PooledObjects.Return(sb);
                    }
                });
        }
        finally
        {
            PooledObjects.Return(namespaces);
        }

        static string CreateFileName(ConcurrentDictionary<string, string> namespaces, IOutputTypeInfo type)
        {
            if (!namespaces.TryGetValue(type.Namespace, out var hashString))
            {
                lock (s_md5)
                {
                    if (!namespaces.TryGetValue(type.Namespace, out hashString))
                    {
                        var bytes = Encoding.UTF8.GetBytes(type.Namespace);
                        var hashBytes = s_md5.ComputeHash(bytes);
                        hashString = Convert.ToBase64String(hashBytes, Base64FormattingOptions.None);
                        hashString = hashString.Replace("+", "-").Replace("/", "_").TrimEnd('=');
                        namespaces.TryAdd(type.Namespace, hashString);
                    }
                }
            }

            return $"{type.Name}.{hashString}.hc.g.cs";
        }
    }

    private static void WriteFile(TypeFileBuilderBase file, IOutputTypeInfo type, ILocalTypeLookup typeLookup)
    {
        file.WriteHeader();
        file.WriteBeginNamespace(type);
        file.WriteBeginClass(type);
        file.WriteInitializeMethod(type, typeLookup);
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
