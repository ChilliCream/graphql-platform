namespace HotChocolate.Resolvers;

/// <summary>
/// Represents the context of the <see cref="IsSelectedVisitor"/>.
/// </summary>
public sealed class IsSelectedContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="IsSelectedContext"/> with the
    /// </summary>
    /// <param name="schema">
    /// The schema that is used to resolve the type of the selection set.
    /// </param>
    /// <param name="selections">
    /// The selection set that is used to determine if a field is selected.
    /// </param>
    public IsSelectedContext(ISchema schema, ISelectionCollection selections)
    {
        Schema = schema;
        Selections.Push(selections);
    }

    /// <summary>
    /// Gets the schema that is used to resolve the type of the selection set.
    /// </summary>
    public ISchema Schema { get; }

    /// <summary>
    /// Gets the selections that is used to determine if a field is selected.
    /// </summary>
    public Stack<ISelectionCollection> Selections { get; } = new();

    /// <summary>
    /// Defines if all fields of the SelectionSet are selected.
    /// </summary>
    public bool AllSelected { get; set; } = true;
}
