namespace HotChocolate.Types;

/// <summary>
/// The mutable definition of an execution group that is collected while the schema is built.
/// </summary>
public sealed class ExecutionGroupConfiguration
{
    /// <summary>
    /// Initializes a new instance of <see cref="ExecutionGroupConfiguration"/>.
    /// </summary>
    public ExecutionGroupConfiguration()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ExecutionGroupConfiguration"/>.
    /// </summary>
    /// <param name="entries">The members of the group.</param>
    public ExecutionGroupConfiguration(IEnumerable<ExecutionGroupEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        Entries.AddRange(entries);
    }

    /// <summary>
    /// Gets the members of the group in definition order.
    /// </summary>
    public List<ExecutionGroupEntry> Entries { get; } = [];
}
