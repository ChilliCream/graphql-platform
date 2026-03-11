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

        for (var i = 0; i < serializers.Count; i++)
        {
            var serializer = serializers[i];
            var serializerName = $"{serializer.CompositeId.Name}NodeIdValueSerializer";

            builder.WriteSerializer(serializer.CompositeId);
            builder.WriteInterceptMethod(i, serializerName, serializer.Location);
        }

        builder.WriteEndClass();
        builder.WriteEndNamespace();

        addSource(WellKnownFileNames.NodeIdSerializerFile, builder.ToString());
    }
}
