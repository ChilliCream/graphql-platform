using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution.Processing;
using HotChocolate.Stitching.SchemaBuilding;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Execution;

internal sealed class StitchingMetadataDb
{
    private readonly List<NameString> _sources;
    private readonly Dictionary<NameString, SourceMetadata> _sourceMetadata = new();

    public StitchingMetadataDb(IEnumerable<NameString> sources, ISchema schema, SchemaInfo schemaInfo)
    {
        if (sources is null)
        {
            throw new ArgumentNullException(nameof(sources));
        }

        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        if (schemaInfo is null)
        {
            throw new ArgumentNullException(nameof(schemaInfo));
        }

        _sources = sources.ToList();

        if (schemaInfo.Query is not null)
        {
            RegisterObjectType(schema, schemaInfo.Query);
        }

        if (schemaInfo.Mutation is not null)
        {
            RegisterObjectType(schema, schemaInfo.Mutation);
        }

        if (schemaInfo.Subscription is not null)
        {
            RegisterObjectType(schema, schemaInfo.Subscription);
        }

        foreach (ObjectTypeInfo objectTypeInfo in schemaInfo.Types.Values.OfType<ObjectTypeInfo>())
        {
            RegisterObjectType(schema, objectTypeInfo);
        }
    }

    private void RegisterObjectType(ISchema schema, ObjectTypeInfo objectTypeInfo)
    {
        if (schema.TryGetType<ObjectType>(objectTypeInfo.Name, out var objectType))
        {
            foreach (var binding in objectTypeInfo.Bindings)
            {
                if (!_sourceMetadata.TryGetValue(binding.Source, out SourceMetadata? metadata))
                {
                    metadata = new();
                    _sourceMetadata.Add(binding.Source, metadata);
                }

                foreach (NameString fieldName in binding.Fields)
                {
                    if (objectType.Fields.TryGetField(fieldName, out IObjectField? field))
                    {
                        metadata.Provides.Add(field);
                    }
                }
            }

            foreach (var group in objectTypeInfo.Fetchers.GroupBy(t => t.Source))
            {
                if (!_sourceMetadata.TryGetValue(group.Key, out SourceMetadata? metadata))
                {
                    metadata = new();
                    _sourceMetadata.Add(group.Key, metadata);
                }

                metadata.Fetchers.Add(objectTypeInfo.Name, group.ToArray());
            }
        }
    }

    public bool IsPartOfSource(NameString source, ISelection selection)
        => _sourceMetadata.TryGetValue(source, out SourceMetadata? metadata) &&
            metadata.Provides.Contains(selection.Field);

    // public NameString GetSource(ISelection selection)
    // {
    //     return Schema.DefaultName;
    // }

    public NameString GetSource(IReadOnlyCollection<ISelection> selections)
    {
        var highestScore = 0;
        NameString? bestMatchingSource = null;

        foreach (NameString source in _sources)
        {
            if (_sourceMetadata.TryGetValue(source, out SourceMetadata? metadata))
            {
                var score = 0;

                foreach (ISelection selection in selections)
                {
                    if (metadata.Provides.Contains(selection.Field))
                    {
                        score++;
                    }
                }

                // we will take the first source that matches all selections.
                if (selections.Count == score)
                {
                    return source;
                }

                // if we cannot match all selections against a single source
                // we will score them and chose the one that resolves the most 
                // of our selections.
                if (highestScore < score)
                {
                    highestScore = score;
                    bestMatchingSource = source;
                }
            }
        }

        // if we do not have a match the schema is inconsistent and we will just fail here.
        if (bestMatchingSource is null)
        {
            throw new InvalidOperationException(
                "We have an inconsistent schema that cannot resolve all possible queries.");
        }

        return bestMatchingSource.Value;
    }

    public ObjectFetcherInfo GetObjectFetcher(
        NameString source,
        IObjectType type,
        IReadOnlyList<IObjectType> typesInPath)
    {
        if (_sourceMetadata.TryGetValue(source, out SourceMetadata? metadata) &&
            metadata.Fetchers.TryGetValue(type.Name, out ObjectFetcherInfo[]? fetchers))
        {
            foreach (ObjectFetcherInfo fetcher in fetchers)
            {
                var matches = true;

                foreach (ArgumentInfo argument in fetcher.Arguments)
                {
                    if (!argument.Binding.Name.Equals(type.Name) &&
                        (typesInPath.Count <= 0 || !typesInPath[0].Name.Equals(type.Name)) &&
                        (typesInPath.Count <= 1 || !typesInPath.Any(t => t.Name.Equals(type.Name))))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    return fetcher;
                }
            }
        }

        throw new InvalidOperationException(
            "We have an inconsistent schema that cannot resolve all possible queries.");
    }

    private sealed class SourceMetadata
    {
        public HashSet<IField> Provides { get; } = new();

        public Dictionary<NameString, ObjectFetcherInfo[]> Fetchers { get; } = new();
    }
}
