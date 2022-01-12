using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

internal readonly struct ObjectFetcherInfo
{
    public ObjectFetcherInfo(
        string source,
        ISyntaxNode selections,
        List<ArgumentInfo> arguments,
        bool batchFetcher = false)
    {
        Source = source ?? throw new System.ArgumentNullException(nameof(source));
        Selections = selections ?? throw new System.ArgumentNullException(nameof(selections));
        Arguments = arguments ?? throw new System.ArgumentNullException(nameof(arguments));
        BatchFetcher = batchFetcher;
    }

    public string Source { get; }

    public ISyntaxNode Selections { get; }

    public List<ArgumentInfo> Arguments { get; }

    public bool BatchFetcher { get; }
}
