namespace HotChocolate.Fusion.Execution.Nodes.Serialization;

/// <summary>
/// Base class for parsers that deserialize an <see cref="OperationPlan"/>
/// from its serialized representation.
/// </summary>
public abstract class OperationPlanParser
{
    /// <summary>
    /// Parses the specified <paramref name="planSourceText"/> into an <see cref="OperationPlan"/>.
    /// </summary>
    /// <param name="planSourceText">The serialized operation plan bytes to parse.</param>
    /// <returns>The deserialized <see cref="OperationPlan"/>.</returns>
    public abstract OperationPlan Parse(ReadOnlyMemory<byte> planSourceText);
}
