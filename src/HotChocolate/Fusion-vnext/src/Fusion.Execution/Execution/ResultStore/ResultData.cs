using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

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

    public virtual void SetNextValueNull()
    {
        throw new NotSupportedException();
    }

    public virtual void SetNextValue(ResultData value)
    {
        throw new NotSupportedException();
    }

    public virtual void SetNextValue(JsonElement value)
    {
        throw new NotSupportedException();
    }

    public virtual bool TrySetValueNull(int index)
    {
        throw new NotSupportedException();
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
