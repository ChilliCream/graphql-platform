namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class SelectionSet
{
    private readonly Selection[] _selections;
    private bool _isSealed;

    public SelectionSet(uint id, Selection[] selections, bool isConditional)
    {
        ArgumentNullException.ThrowIfNull(selections);

        if (selections.Length == 0)
        {
            throw new ArgumentException("Selections cannot be empty.", nameof(selections));
        }

        Id = id;
        IsConditional = isConditional;
        _selections = selections;
    }

    /// <summary>
    /// Gets an operation unique selection-set identifier of this selection.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Defines if this list needs post-processing for skip and include.
    /// </summary>
    public bool IsConditional { get; }

    /// <summary>
    /// Gets the selections that shall be executed.
    /// </summary>
    public ReadOnlySpan<Selection> Selections => _selections;

    /// <summary>
    /// Gets the declaring operation.
    /// </summary>
    public Operation DeclaringOperation { get; private set; } = null!;

    internal void Seal(Operation operation)
    {
        if (_isSealed)
        {
            throw new InvalidOperationException("Selection set is already sealed.");
        }

        _isSealed = true;
        DeclaringOperation = operation;

        foreach (var selection in Selections)
        {
            selection.Seal(this);
        }
    }
}
