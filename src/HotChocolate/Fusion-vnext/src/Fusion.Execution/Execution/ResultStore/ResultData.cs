using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents a result data object like an object or list.
/// </summary>
public abstract class ResultData
{
    /// <summary>
    /// Gets the parent result data object.
    /// </summary>
    protected internal ResultData? Parent { get; private set; }

    /// <summary>
    /// Gets the index under which this data is stored in the parent result.
    /// </summary>
    protected internal int ParentIndex { get; private set; }

    /// <summary>
    /// Connects this result to the parent result.
    /// </summary>
    /// <param name="parent">
    /// The parent result.
    /// </param>
    /// <param name="index">
    /// The index under which this result is stored in the parent result.
    /// </param>
    protected internal void SetParent(ResultData parent, int index)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        Parent = parent;
        ParentIndex = index;
    }

    /// <summary>
    /// Resets the parent and parent index.
    /// </summary>
    public virtual void Reset()
    {
        Parent = null;
        ParentIndex = -1;
    }
}

// Lis<List> / List<JsonElement> / List<Object>
// Field<List> / Field<JsonElement> / Field<Object>

public sealed class ObjectResult : ResultData, IReadOnlyDictionary<string, FieldResult>
{
    private readonly Dictionary<string, FieldResult> _fieldMap = [];
    private FieldResult[] _buffer = new FieldResult[128];

    public SelectionSet SelectionSet { get; private set; } = null!;

    public bool IsInitialized { get; private set; }

    public int Count => _fieldMap.Count;

    public FieldResult this[string key] => _fieldMap[key];

    public IEnumerable<string> Keys => _fieldMap.Keys;

    public IEnumerable<FieldResult> Values => _fieldMap.Values;

    public bool ContainsKey(string key) => _fieldMap.ContainsKey(key);

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out FieldResult value)
        => _fieldMap.TryGetValue(key, out value);

    public ReadOnlySpan<FieldResult> AsSpan() => _buffer.AsSpan();

    public IEnumerator<KeyValuePair<string, FieldResult>> GetEnumerator()
        => _fieldMap.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Initialize(SelectionSet selectionSet, uint includeFlags)
    {
        ArgumentNullException.ThrowIfNull(selectionSet);

        SelectionSet = selectionSet;
        IsInitialized = true;

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

            var field = CreateFieldResult(selection);
            _buffer[i] = field;
            _fieldMap.Add(selection.ResponseName, field);
        }

        static FieldResult CreateFieldResult(Selection selection)
        {
            FieldResult field;

            if (selection.Field.Type.IsListType())
            {
                field = new ListFieldResult();
            }
            else if (selection.Field.Type.NamedType().IsLeafType())
            {
                field = new LeafFieldResult();
            }
            else
            {
                field = new ObjectFieldResult();
            }

            field.Initialize(selection);

            return field;
        }
    }

    public override void Reset()
    {
        SelectionSet = null!;
        IsInitialized = false;

        for (var i = 0; i < _buffer.Length; i++)
        {
            _buffer[i] = null!;
        }

        _fieldMap.Clear();

        base.Reset();
    }

}

public abstract class FieldResult : ResultData
{
    public Selection Selection { get; protected set; } = null!;

    protected internal virtual void Initialize(Selection selection)
    {
        Selection = selection;
    }

    public override void Reset()
    {
        Selection = null!;
    }
}

public class LeafFieldResult : FieldResult
{
    public JsonElement Value { get; set; }
}

public class ListFieldResult : FieldResult
{
    public ListResult? Value { get; set; }
}

public class ObjectFieldResult : FieldResult
{
    public ObjectResult? Value { get; set; }
}

public class ListResult : ResultData;

public class ObjectListResult : ListResult
{
    public List<ObjectResult?> Items { get; } = [];
}

public class NestedListResult : ListResult
{
    public List<ListResult?> Items { get; } = [];
}

public class LeafListResult : ListResult
{
    public List<JsonElement> Items { get; } = [];
}

public sealed class Selection
{
    public uint Id { get; }

    public string ResponseName { get; }

    public int ResponseIndex { get; }

    public IOutputFieldDefinition Field { get; }

    public IType Type => Field.Type;

    public SelectionSet DeclaringSelectionSet { get; }

    public SelectionSetNode? SelectionSet { get; }

    public bool IsIncluded(long includeFlags)
        => throw new NotImplementedException();

}

public sealed class SelectionSet
{
    /// <summary>
    /// Gets an operation unique selection-set identifier of this selection.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Defines if this list needs post-processing for skip and include.
    /// </summary>
    bool IsConditional { get; }

    /// <summary>
    /// Gets the selections that shall be executed.
    /// </summary>
    public ReadOnlySpan<Selection> Selections => throw new NotImplementedException();

    /// <summary>
    /// Gets the declaring operation.
    /// </summary>
    public Operation DeclaringOperation { get; }
}

public sealed class Operation
{
    /// <summary>
    /// Gets the internal unique identifier for this operation.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the parsed query document that contains the
    /// operation-<see cref="Definition" />.
    /// </summary>
    public DocumentNode Document { get; }

    /// <summary>
    /// Gets the syntax node representing the operation definition.
    /// </summary>
    public OperationDefinitionNode Definition { get; }

    /// <summary>
    /// Gets the root type on which the operation is executed.
    /// </summary>
    public IObjectTypeDefinition RootType { get; }

    /// <summary>
    /// Gets the schema for which this operation is compiled.
    /// </summary>
    public ISchemaDefinition Schema { get; }

    /// <summary>
    /// Gets the name of the operation.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the prepared root selections for this operation.
    /// </summary>
    /// <returns>
    /// Returns the prepared root selections for this operation.
    /// </returns>
    public SelectionSet RootSelectionSet { get; }

    /// <summary>
    /// Gets the selection set for the specified <paramref name="selection"/> and
    /// <paramref name="typeContext"/>.
    /// </summary>
    /// <param name="selection">
    /// The selection set for which the selection set shall be resolved.
    /// </param>
    /// <param name="typeContext">
    /// The result type context.
    /// </param>
    /// <returns>
    /// Returns the selection set for the specified <paramref name="selection"/> and
    /// <paramref name="typeContext"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The specified <paramref name="selection"/> has no selection set.
    /// </exception>
    public SelectionSet GetSelectionSet(Selection selection, IObjectTypeDefinition typeContext)
    {
        throw new NotImplementedException();
    }

    public SelectionSet GetSelectionSet(Path path)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the possible return types for the <paramref name="selection"/>.
    /// </summary>
    /// <param name="selection">
    /// The selection for which the possible result types shall be returned.
    /// </param>
    /// <returns>
    /// Returns the possible return types for the specified <paramref name="selection"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The specified <paramref name="selection"/> has no selection set.
    /// </exception>
    public IEnumerable<IObjectTypeDefinition> GetPossibleTypes(Selection selection)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Creates the include flags for the specified variable values.
    /// </summary>
    /// <param name="variables">
    /// The variable values.
    /// </param>
    /// <returns>
    /// Returns the include flags for the specified variable values.
    /// </returns>
    public long CreateIncludeFlags(IVariableValueCollection variables)
    {
        throw new NotImplementedException();
    }
}
