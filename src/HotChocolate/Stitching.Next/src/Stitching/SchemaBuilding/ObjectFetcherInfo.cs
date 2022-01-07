using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

internal readonly struct ObjectFetcherInfo
{
    public ObjectFetcherInfo(
        string source,
        ISyntaxNode selections,
        List<ArgumentInfo> arguments)
    {

        Source = source ?? throw new System.ArgumentNullException(nameof(source));
        Selections = selections ?? throw new System.ArgumentNullException(nameof(selections));
        Arguments = arguments ?? throw new System.ArgumentNullException(nameof(arguments));
    }

    public string Source { get; }

    public ISyntaxNode Selections { get; }

    public List<ArgumentInfo> Arguments { get; }
}
