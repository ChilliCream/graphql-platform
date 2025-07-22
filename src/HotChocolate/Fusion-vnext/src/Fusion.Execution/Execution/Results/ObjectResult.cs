using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents an object result.
/// </summary>
public sealed class ObjectResult : ResultData, IReadOnlyDictionary<string, object?>
{
    private int _maxAllowedCapacity = 512;
    private readonly Dictionary<string, FieldResult> _fieldMap = [];
    private FieldResult[] _fields = [];

    /// <summary>
    /// Gets the selection set represents the structure of this object result.
    /// </summary>
    public SelectionSet SelectionSet { get; private set; } = null!;

    /// <summary>
    /// Gets the field result for the given key.
    /// </summary>
    public FieldResult this[string responseName] => _fieldMap[responseName];

    /// <summary>
    /// Gets the fields of the object result.
    /// </summary>
    public ReadOnlySpan<FieldResult> Fields => _fields.AsSpan(0, _fieldMap.Count);

    /// <summary>
    /// Gets the number of fields in the object result.
    /// </summary>
    public int Count => _fieldMap.Count;

    /// <summary>
    /// Checks if the object result contains a field with the given response name.
    /// </summary>
    /// <param name="responseName">
    /// The response name of the field.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the object result contains a field with the given response name; otherwise, <see langword="false"/>.
    /// </returns>
    public bool ContainsKey(string responseName) => _fieldMap.ContainsKey(responseName);

    /// <summary>
    /// Tries to get the field result for the given response name.
    /// </summary>
    /// <param name="responseName">
    /// The response name of the field.
    /// </param>
    /// <param name="value">
    /// The field result.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the field result was found; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetValue(string responseName, [MaybeNullWhen(false)] out FieldResult value)
        => _fieldMap.TryGetValue(responseName, out value);

    internal void MoveFieldTo(string fieldName, ObjectResult target)
    {
        var field = _fieldMap[fieldName];
        field.SetParent(target, field.ParentIndex);
        target._fields[field.ParentIndex] = field;
        target._fieldMap[fieldName] = field;
    }

    /// <summary>
    /// Writes the object result to the specified JSON writer.
    /// </summary>
    /// <param name="writer">
    /// The JSON writer to write the object result to.
    /// </param>
    /// <param name="options">
    /// The serializer options.
    /// If options are set to null <see cref="JsonSerializerOptions"/>.Web will be used.
    /// </param>
    /// <param name="nullIgnoreCondition">
    /// The null ignore condition.
    /// </param>
    public override void WriteTo(
        Utf8JsonWriter writer,
        JsonSerializerOptions? options = null,
        JsonNullIgnoreCondition nullIgnoreCondition = JsonNullIgnoreCondition.None)
    {
        writer.WriteStartObject();

        var fields = _fields.AsSpan(0, _fieldMap.Count);
        ref var field = ref MemoryMarshal.GetReference(fields);
        ref var end = ref Unsafe.Add(ref field, fields.Length);

        while (Unsafe.IsAddressLessThan(ref field, ref end))
        {
            // Internal fields represent data that is needed to execute the
            // GraphQL operation itself and are not part of the response.
            if (!field.Selection.IsInternal)
            {
                field.WriteTo(writer, options, nullIgnoreCondition);
            }

            field = ref Unsafe.Add(ref field, 1)!;
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Initializes the object result.
    /// </summary>
    /// <param name="resultPoolSession">
    /// The result pool session.
    /// </param>
    /// <param name="selectionSet">
    /// The selection set.
    /// </param>
    /// <param name="includeFlags">
    /// The include flags.
    /// </param>
    /// <param name="rawLeafFields">
    /// Leaf fields will be stored as raw memory.
    /// </param>
    public void Initialize(
        ResultPoolSession resultPoolSession,
        SelectionSet selectionSet,
        ulong includeFlags,
        bool rawLeafFields = false)
    {
        ArgumentNullException.ThrowIfNull(resultPoolSession);
        ArgumentNullException.ThrowIfNull(selectionSet);

        SelectionSet = selectionSet;

        if (_fields.Length < selectionSet.Selections.Length)
        {
            _fieldMap.EnsureCapacity(selectionSet.Selections.Length);
            _fields = new FieldResult[selectionSet.Selections.Length];
        }

        var insertIndex = 0;
        for (var i = 0; i < selectionSet.Selections.Length; i++)
        {
            var selection = selectionSet.Selections[i];

            if (!selection.IsIncluded(includeFlags))
            {
                continue;
            }

            var ii = insertIndex++;
            var field = CreateFieldResult(this, ii, resultPoolSession, selection, rawLeafFields);
            _fields[ii] = field;
            _fieldMap.Add(selection.ResponseName, field);
        }

        static FieldResult CreateFieldResult(
            ResultData parent,
            int parentIndex,
            ResultPoolSession resultPoolSession,
            Selection selection,
            bool rawLeafFields)
        {
            FieldResult field;

            if (selection.Field.Type.IsListType())
            {
                field = resultPoolSession.RentListFieldResult();
            }
            else if (selection.IsLeaf)
            {
                if (rawLeafFields)
                {
                    // TODO : shall we pool these as well?
                    field = new RawFieldResult();
                }
                else
                {
                    field = resultPoolSession.RentLeafFieldResult();
                }
            }
            else
            {
                field = resultPoolSession.RentObjectFieldResult();
            }

            field.Initialize(parent, parentIndex, selection);

            return field;
        }
    }

    internal override void SetCapacity(int capacity, int maxAllowedCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 8);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAllowedCapacity, 16);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(capacity, maxAllowedCapacity);

        _maxAllowedCapacity = maxAllowedCapacity;
        _fieldMap.EnsureCapacity(capacity);
        _fields = new FieldResult[capacity];
    }

    /// <summary>
    /// Resets the object result.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the object result was reset; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Reset()
    {
        SelectionSet = null!;

        if (_fieldMap.Count > _fields.Length)
        {
            return false;
        }

        for (var i = 0; i < _fieldMap.Count; i++)
        {
            _fields[i] = null!;
        }

#if NET9_0_OR_GREATER
        _fieldMap.Clear();
        return base.Reset() && _fieldMap.Capacity < _maxAllowedCapacity;
#else
        var retainResult = _fieldMap.Count < _maxAllowedCapacity;
        _fieldMap.Clear();
        return base.Reset() && retainResult;
#endif
    }

    object? IReadOnlyDictionary<string, object?>.this[string key]
        => this[key].AsKeyValuePair().Value;

    IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys
        => _fieldMap.Keys;

    IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values
        => _fieldMap.Values.Select(t => t.AsKeyValuePair().Value);

    bool IReadOnlyDictionary<string, object?>.TryGetValue(string key, out object? value)
    {
        if (_fieldMap.TryGetValue(key, out var field))
        {
            value = field.AsKeyValuePair().Value;
            return true;
        }

        value = null;
        return false;
    }

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
    {
        for (var i = 0; i < _fieldMap.Count; i++)
        {
            yield return _fields[i].AsKeyValuePair();
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        for (var i = 0; i < _fieldMap.Count; i++)
        {
            yield return _fields[i].AsKeyValuePair();
        }
    }
}
