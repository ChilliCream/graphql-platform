using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution.Results;

/// <summary>
/// The additional targets of a lookup, kept as cursors into the result document until their paths
/// are requested.
/// </summary>
internal sealed class TargetCursorPaths : CompactPathSource
{
    private readonly FetchResultStore _store;
    private readonly CompositeResultDocument _document;
    private readonly int[] _cursors;
    private readonly int _structureVersion;
    private CompactPath[]? _paths;

    public TargetCursorPaths(FetchResultStore store, CompositeResultDocument document, int[] cursors)
    {
        _store = store;
        _document = document;
        _cursors = cursors;
        _structureVersion = document.StructureVersion;
    }

    /// <summary>
    /// Gets the result document the cursors point into.
    /// </summary>
    public CompositeResultDocument Document => _document;

    /// <summary>
    /// Gets <c>true</c> while every cursor still points to the element at its path.
    /// </summary>
    public bool IsCurrent => _document.StructureVersion == _structureVersion;

    /// <summary>
    /// Gets the element at the cursor with the given index.
    /// </summary>
    public CompositeResultElement GetElement(int index)
        => new(_document, new CompositeResultDocument.Cursor(_cursors[index]));

    public override CompactPath[] GetPaths()
    {
        if (Volatile.Read(ref _paths) is { } paths)
        {
            return paths;
        }

        paths = _store.CreateCompactPaths(_document, _cursors);
        return Interlocked.CompareExchange(ref _paths, paths, null) ?? paths;
    }
}
