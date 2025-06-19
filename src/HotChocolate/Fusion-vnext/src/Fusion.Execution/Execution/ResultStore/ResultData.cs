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
