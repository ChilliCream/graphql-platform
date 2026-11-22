using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution.Results;

/// <summary>
/// Creates the paths of lookup targets that are kept as cursors into the result document of a
/// store.
/// </summary>
internal sealed class TargetCursorPathSource
{
    private readonly FetchResultStore _store;
    private readonly CompositeResultDocument _document;
    private readonly int _layoutVersion;

    public TargetCursorPathSource(FetchResultStore store, CompositeResultDocument document)
    {
        _store = store;
        _document = document;
        _layoutVersion = document.LayoutVersion;
    }

    /// <summary>
    /// Gets the result document the cursors point into.
    /// </summary>
    public CompositeResultDocument Document => _document;

    /// <summary>
    /// Gets <c>true</c> while every cursor still points to the element at its path.
    /// </summary>
    public bool IsCurrent => _document.LayoutVersion == _layoutVersion;

    /// <summary>
    /// Gets the element at <paramref name="cursor"/>.
    /// </summary>
    public CompositeResultElement GetElement(int cursor)
        => new(_document, new CompositeResultDocument.Cursor(cursor));

    /// <summary>
    /// Creates the path of the element at <paramref name="cursor"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The document is no longer the result document of the store.
    /// </exception>
    public CompactPath CreatePath(int cursor)
        => _store.CreateCompactPath(_document, cursor);
}
