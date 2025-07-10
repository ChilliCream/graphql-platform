using System.Text.Json;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents a result data object like an object or list.
/// </summary>
public abstract class ResultData : IResultDataJsonFormatter
{
    private Path? _path;

    /// <summary>
    /// Gets the parent result data object.
    /// </summary>
    protected internal ResultData? Parent { get; private set; }

    /// <summary>
    /// Gets the index under which this data is stored in the parent result.
    /// </summary>
    protected internal int ParentIndex { get; private set; }

    /// <summary>
    /// Gets the path of the result.
    /// </summary>
    public Path Path
    {
        get
        {
            if (_path is null)
            {
                var stack = new Stack<ResultData>();
                var current = this;

                while (current is not null)
                {
                    stack.Push(current);
                    current = current.Parent;
                }

                var path = Path.Root;

                while (stack.TryPop(out var item))
                {
                    if (item.Parent is null)
                    {
                        continue;
                    }

                    path = item.Parent switch
                    {
                        ObjectResult obj => path.Append(obj.Fields[item.ParentIndex].Selection.ResponseName),
                        ListResult => path.Append(item.ParentIndex),
                        _ => path
                    };
                }

                _path = path;
            }

            return _path;
        }
    }

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
    /// Sets the next value to <see langword="null"/>.
    /// </summary>
    public virtual void SetNextValueNull()
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Sets the next value to the given value.
    /// </summary>
    /// <param name="value">The value to set.</param>
    public virtual void SetNextValue(ResultData value)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Sets the next value to the given value.
    /// </summary>
    /// <param name="value">The value to set.</param>
    public virtual void SetNextValue(JsonElement value)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Tries to set the next value to <see langword="null"/>.
    /// </summary>
    /// <param name="index">The index to set.</param>
    /// <returns>
    /// <see langword="true"/> if the value was set; otherwise, <see langword="false"/>.
    /// </returns>
    public virtual bool TrySetValueNull(int index)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public abstract void WriteTo(
        Utf8JsonWriter writer,
        JsonSerializerOptions? options = null,
        JsonNullIgnoreCondition nullIgnoreCondition = JsonNullIgnoreCondition.None);

    /// <summary>
    /// Sets the capacity of the result data.
    /// </summary>
    /// <param name="capacity">
    /// The capacity of the result data.
    /// </param>
    /// <param name="maxAllowedCapacity">
    /// The maximum allowed capacity of the result data.
    /// </param>
    internal virtual void SetCapacity(int capacity, int maxAllowedCapacity)
    {
    }

    /// <summary>
    /// Resets the parent and parent index.
    /// </summary>
    public virtual bool Reset()
    {
        Parent = null;
        ParentIndex = -1;
        _path = null;
        return true;
    }
}
