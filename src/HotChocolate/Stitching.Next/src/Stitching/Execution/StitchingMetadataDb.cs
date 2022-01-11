using System;
using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Stitching.SchemaBuilding;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Execution;

internal sealed class StitchingMetadataDb
{
    private readonly List<NameString> _sources;
    private readonly Dictionary<NameString, SourceMetadata> _sourceMetadata;

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
            // todo: ensure that we prefer batch fetcher with the least hierarchy dependencies.
            foreach (ObjectFetcherInfo fetcher in fetchers)
            {
                //  foreach()
            }
        }

        throw new InvalidOperationException(
            "We have an inconsistent schema that cannot resolve all possible queries.");
    }

    private sealed class SourceMetadata
    {
        public SourceMetadata(
            HashSet<IField> provides,
            Dictionary<NameString, ObjectFetcherInfo[]> fetchers)
        {
            Provides = provides;
            Fetchers = fetchers;
        }

        public HashSet<IField> Provides { get; }

        public Dictionary<NameString, ObjectFetcherInfo[]> Fetchers { get; }
    }
}
