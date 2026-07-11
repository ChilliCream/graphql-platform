using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.FileBuilders;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Generators;

public sealed class NodeIdValueSerializerGenerator : ISyntaxGenerator
{
    public void Generate(
        SourceProductionContext context,
        string assemblyName,
        ImmutableArray<SyntaxInfo> syntaxInfos,
        Action<string, string> addSource)
    {
        var serializers = new List<NodeIdValueSerializerInfo>();

        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is NodeIdValueSerializerInfo info && info.Diagnostics.Length == 0)
            {
                serializers.Add(info);
            }
        }

        if (serializers.Count == 0)
        {
            return;
        }

        using var builder = new NodeIdValueSerializerFileBuilder();

        builder.WriteHeader();
        builder.WriteBeginNamespace();
        builder.WriteBeginClass();

        // We deduplicate serializer classes by fully qualified type name so that
        // multiple call sites for the same type only emit a single class definition.
        var emittedSerializers = new Dictionary<string, string>(StringComparer.Ordinal);

        for (var i = 0; i < serializers.Count; i++)
        {
            var serializer = serializers[i];
            var fullyQualified = serializer.OrderByKey;

            if (!emittedSerializers.TryGetValue(fullyQualified, out var serializerName))
            {
                serializerName = GetUniqueSerializerName(
                    serializer.CompositeId.Name,
                    emittedSerializers);
                emittedSerializers[fullyQualified] = serializerName;
                builder.WriteSerializer(serializerName, serializer.CompositeId);
            }

            builder.WriteInterceptMethod(i, serializerName, serializer.Location);
        }

        builder.WriteEndClass();
        builder.WriteEndNamespace();

        addSource(WellKnownFileNames.NodeIdSerializerFile, builder.ToString());
    }

    private static string GetUniqueSerializerName(
        string shortName,
        Dictionary<string, string> emittedSerializers)
    {
        var candidate = $"{shortName}NodeIdValueSerializer";

        // We check for name collisions (e.g. two types with the same short name
        // in different namespaces) and append a numeric suffix if needed.
        if (!emittedSerializers.ContainsValue(candidate))
        {
            return candidate;
        }

        var suffix = 2;

        while (emittedSerializers.ContainsValue($"{candidate}{suffix}"))
        {
            suffix++;
        }

        return $"{candidate}{suffix}";
    }
}
