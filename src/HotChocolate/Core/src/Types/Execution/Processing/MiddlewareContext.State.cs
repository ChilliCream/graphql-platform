using System.Collections.Immutable;
using HotChocolate.Text.Json;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing;

internal partial class MiddlewareContext
{
    private object? _result;
    private object? _parent;
    private Path? _path;

    public Path Path => _path ??= ResultValue.Path;

    public IImmutableDictionary<string, object?> ScopedContextData { get; set; } = null!;

    public IImmutableDictionary<string, object?> LocalContextData { get; set; } = null!;

    // TODO : Remove?
    public IType? ValueType { get; set; }

    public ResultElement ResultValue { get; private set; }

    public bool HasErrors { get; private set; }

    public object? Result
    {
        get => _result;
        set
        {
            _result = value;
            IsResultModified = true;
        }
    }

    public bool IsResultModified { get; private set; }

    public T Parent<T>()
    {
        if (_parent is T casted)
        {
            return casted;
        }

        if (_parent is null)
        {
            return default!;
        }

        if (_operationContext.Converter.TryConvert(_parent, out casted))
        {
            return casted;
        }

        throw ThrowHelper.ResolverContext_CannotCastParent(
            Selection.Field.Coordinate,
            Path,
            typeof(T),
            _parent.GetType());
    }
}
