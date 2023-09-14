using System.Collections.Immutable;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing;

internal partial class MiddlewareContext
{
    private object? _result;
    private object? _parent;
    private Path? _path;

    public Path Path => _path ??= PathHelper.CreatePathFromContext(Selection, ParentResult, -1);

    public IImmutableDictionary<string, object?> ScopedContextData { get; set; } = default!;

    public IImmutableDictionary<string, object?> LocalContextData { get; set; } = default!;

    public IType? ValueType { get; set; }

    public ObjectResult ParentResult { get; private set; } = default!;

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
