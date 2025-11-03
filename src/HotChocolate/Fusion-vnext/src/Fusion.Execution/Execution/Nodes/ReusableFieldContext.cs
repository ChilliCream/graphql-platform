using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

internal sealed class ReusableFieldContext(
    ISchemaDefinition schema,
    IVariableValueCollection variableValues,
    ulong includeFlags,
    PooledArrayWriter memory)
    : FieldContext
{
    private readonly Dictionary<string, IValueNode> _arguments = [];
    private readonly List<object?> _runtimeResults = [];
    private Selection _selection = null!;
    private object? _parent;
    private SourceResultElementBuilder _result = default!;

    public override PooledArrayWriter Memory => memory;

    public override ISchemaDefinition Schema => schema;

    public override Selection Selection => _selection;

    public override SourceResultElementBuilder FieldResult => _result;

    public List<object?> RuntimeResults => _runtimeResults;

    public override ulong IncludeFlags => includeFlags;

    public override T Parent<T>() => (T)_parent!;

    public override T ArgumentValue<T>(string name)
    {
        if (_arguments.TryGetValue(name, out var value))
        {
            if (value is T casted)
            {
                return casted;
            }

            // todo: add proper exception.
            throw new Exception("Invalid argument value!");
        }

        // todo: add proper exception.
        throw new Exception("Invalid argument name!");
    }

    public override void AddRuntimeResult<T>(T result)
    {
        _runtimeResults.Add(result);
    }

    public void Initialize(object? parent, Selection selection, SourceResultElementBuilder result)
    {
        _parent = parent;
        _result = result;
        _selection = selection;
        _runtimeResults.Clear();
        CoerceArgumentValues(selection);
    }

    private void CoerceArgumentValues(Selection selection)
    {
        _arguments.Clear();

        if (selection.Field.Arguments.Count == 0)
        {
            return;
        }

        var syntaxNode = selection.SyntaxNodes[0].Node;

        foreach (var argument in selection.Field.Arguments)
        {
            var argumentValue = syntaxNode.Arguments.FirstOrDefault(
                t => t.Name.Value.Equals(argument.Name, StringComparison.Ordinal))
                    ?.Value;

            if (argumentValue is VariableNode variable
                && variableValues.TryGetValue(variable.Name.Value, out IValueNode? variableValue))
            {
                argumentValue = variableValue;
            }

            argumentValue ??= argument.DefaultValue;

            if (argument.Type.IsNonNullType() && argumentValue is null or NullValueNode)
            {
                // TODO: The argument value is invalid.
                throw new Exception("The argument value is invalid.");
            }

            _arguments.Add(argument.Name, argumentValue ?? NullValueNode.Default);
        }
    }
}
