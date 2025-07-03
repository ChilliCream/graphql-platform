using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution;

public abstract class FieldResult : ResultData
{
    /// <summary>
    /// <para>
    /// Gets the GraphQL field selection that this result corresponds to.
    /// </para>
    /// <para>
    /// The <see cref="Selection"/> property identifies the GraphQL field selection in the GraphQL operation
    /// that produced this result.
    /// </para>
    /// </summary>
    public Selection Selection { get; protected set; } = null!;

    /// <summary>
    /// Gets the key-value pair representation of this result.
    /// </summary>
    /// <returns>
    /// A <see cref="KeyValuePair{TKey,TValue}"/> where the key is the name of the field and the value is the result.
    /// </returns>
    protected internal abstract KeyValuePair<string, object?> AsKeyValuePair();

    /// <summary>
    /// Initializes the <see cref="FieldResult"/> with the specified selection.
    /// </summary>
    /// <param name="selection">
    /// The GraphQL field selection that this result corresponds to.
    /// </param>
    protected internal virtual void Initialize(Selection selection)
    {
        Selection = selection;
    }

    /// <summary>
    /// Resets the <see cref="FieldResult"/> to its initial state.
    /// </summary>
    public override bool Reset()
    {
        Selection = null!;
        return base.Reset();
    }
}
