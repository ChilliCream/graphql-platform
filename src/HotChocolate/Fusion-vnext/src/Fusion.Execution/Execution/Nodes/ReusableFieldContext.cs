using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Execution.Nodes;

internal sealed class ReusableFieldContext(
    ResultPoolSession resultPool,
    PooledArrayWriter memory,
    ISchemaDefinition schema)
    : FieldContext
{
    private readonly List<object?> _runtimeResults = [];
    private Selection _selection = null!;
    private object? _parent;
    private FieldResult _result = null!;

    public override ResultPoolSession ResultPool => resultPool;

    public override PooledArrayWriter Memory => memory;

    public override ISchemaDefinition Schema => schema;

    public override Selection Selection => _selection;

    public override FieldResult FieldResult => _result;

    public List<object?> RuntimeResults => _runtimeResults;

    public override T Parent<T>() => (T)_parent!;

    public override T ArgumentValue<T>(string name)
    {
        throw new NotImplementedException();
    }

    public override void AddRuntimeResult<T>(T result)
    {
        _runtimeResults.Add(result);
    }

    public void Initialize(object? parent, Selection selection, FieldResult result)
    {
        _parent = parent;
        _result = result;
        _selection = selection;
        _runtimeResults.Clear();
    }
}
