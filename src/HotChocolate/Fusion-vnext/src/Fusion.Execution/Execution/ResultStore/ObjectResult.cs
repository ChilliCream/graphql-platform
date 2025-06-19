using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

public sealed class ObjectResult : ResultData, IReadOnlyDictionary<string, FieldResult>
{
    private readonly Dictionary<string, FieldResult> _fieldMap = [];
    private FieldResult[] _buffer = new FieldResult[128];

    public SelectionSet SelectionSet { get; private set; } = null!;

    public bool IsInitialized { get; private set; }

    public int Count => _fieldMap.Count;

    public FieldResult this[string key] => _fieldMap[key];

    IEnumerable<string> IReadOnlyDictionary<string, FieldResult>.Keys => _fieldMap.Keys;

    IEnumerable<FieldResult> IReadOnlyDictionary<string, FieldResult>.Values => _fieldMap.Values;

    public ReadOnlySpan<FieldResult> Fields => _buffer.AsSpan();

    public bool ContainsKey(string key) => _fieldMap.ContainsKey(key);

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out FieldResult value)
        => _fieldMap.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<string, FieldResult>> GetEnumerator()
        => _fieldMap.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Initialize(ResultPoolSession resultPoolSession, SelectionSet selectionSet, uint includeFlags)
    {
        ArgumentNullException.ThrowIfNull(resultPoolSession);
        ArgumentNullException.ThrowIfNull(selectionSet);

        IsInitialized = true;
        SelectionSet = selectionSet;

        if (_buffer.Length < selectionSet.Selections.Length)
        {
            Array.Resize(ref _buffer, selectionSet.Selections.Length);
        }

        for (var i = 0; i < selectionSet.Selections.Length; i++)
        {
            var selection = selectionSet.Selections[i];

            if (!selection.IsIncluded(includeFlags))
            {
                continue;
            }

            var field = CreateFieldResult(resultPoolSession, selection);
            _buffer[i] = field;
            _fieldMap.Add(selection.ResponseName, field);
        }

        static FieldResult CreateFieldResult(ResultPoolSession resultPoolSession, Selection selection)
        {
            FieldResult field;

            if (selection.Field.Type.IsListType())
            {
                field = resultPoolSession.RentListFieldResult();
            }
            else if (selection.Field.Type.NamedType().IsLeafType())
            {
                field = resultPoolSession.RentLeafFieldResult();
            }
            else
            {
                field = resultPoolSession.RentObjectFieldResult();
            }

            field.Initialize(selection);

            return field;
        }
    }

    public override void Reset()
    {
        IsInitialized = false;
        SelectionSet = null!;

        for (var i = 0; i < _buffer.Length; i++)
        {
            _buffer[i] = null!;
        }

        _fieldMap.Clear();

        base.Reset();
    }
}
