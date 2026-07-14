namespace HotChocolate.Fusion.Execution.Nodes.Serialization;

/// <summary>
/// Base class for formatters that serialize an <see cref="OperationPlan"/>
/// into a human- or machine-readable string representation.
/// </summary>
public abstract class OperationPlanFormatter
{
    /// <summary>
    /// Formats the specified <paramref name="plan"/> as a string.
    /// </summary>
    /// <param name="plan">The operation plan to format.</param>
    /// <param name="trace">Optional trace information to include in the output.</param>
    /// <returns>A string representation of the operation plan.</returns>
    public abstract string Format(OperationPlan plan, OperationPlanTrace? trace = null);
}
