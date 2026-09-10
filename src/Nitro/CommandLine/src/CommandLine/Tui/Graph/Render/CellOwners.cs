namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Render;

/// <summary>
/// Provides the owners of a cell without allocating for the usual single-owner case.
/// </summary>
internal readonly struct CellOwners(object? first, List<object>? additional) : IReadOnlyList<object>
{
    private readonly object? _first = first;
    private readonly List<object>? _additional = additional;

    public int Count => _first is null ? 0 : 1 + (_additional?.Count ?? 0);

    public object this[int index] => index == 0 && _first is not null ? _first : _additional![index - 1];

    public Enumerator GetEnumerator() => new(_first, _additional);

    IEnumerator<object> IEnumerable<object>.GetEnumerator() => GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    internal struct Enumerator(object? first, List<object>? additional) : IEnumerator<object>
    {
        private readonly object? _first = first;
        private readonly List<object>? _additional = additional;
        private int _index = -1;

        public object Current => _index == 0 && _first is not null
            ? _first
            : _additional![_index - 1];

        object System.Collections.IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _index++;
            return _index < (_first is null ? 0 : 1 + (_additional?.Count ?? 0));
        }

        public void Reset() => _index = -1;

        public void Dispose()
        {
        }
    }
}
