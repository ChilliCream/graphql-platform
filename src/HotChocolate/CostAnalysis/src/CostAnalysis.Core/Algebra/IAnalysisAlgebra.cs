using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Defines how field summaries combine into an operation analysis.
/// </summary>
/// <remarks>
/// Summary values must have a consistent ordering.
/// <see cref="Combine"/> must be associative, commutative, preserve that ordering,
/// and use <see cref="Empty"/> as its identity.
/// <see cref="Join"/> must return an upper bound of both inputs.
/// Applying <see cref="Field"/> or <see cref="Combine"/> to joined inputs must produce
/// an upper bound of the results obtained by applying them to each alternative separately.
/// <see cref="Join"/> need not be idempotent or return the smallest upper bound.
/// </remarks>
/// <typeparam name="TSummary">
/// The result type for fields, selection sets, and operations.
/// </typeparam>
[Experimental(CostExperiments.AnalysisAlgebra)]
public interface IAnalysisAlgebra<TSummary>
{
    /// <summary>
    /// Gets the summary that leaves another summary unchanged when combined with it.
    /// </summary>
    TSummary Empty { get; }

    /// <summary>
    /// Computes the summary contributed by one collected field group.
    /// </summary>
    /// <param name="group">
    /// A field selection and its possible parent type.
    /// </param>
    /// <param name="child">
    /// The combined summary of the group's own selection set.
    /// </param>
    TSummary Field(in CollectedFieldGroup group, TSummary child);

    /// <summary>
    /// Combines two summaries collected within the same selection boundary.
    /// </summary>
    TSummary Combine(TSummary left, TSummary right);

    /// <summary>
    /// Computes the upper bound of two summaries from mutually exclusive
    /// type regions or Boolean-decision branches.
    /// </summary>
    TSummary Join(TSummary left, TSummary right);

    /// <summary>
    /// Computes an operation's final summary from its root type's own weight
    /// and its root selection set's combined summary.
    /// </summary>
    /// <param name="rootTypeWeight">
    /// The operation's root type's own weight.
    /// </param>
    /// <param name="selection">
    /// The root selection set's combined summary.
    /// </param>
    /// <remarks>
    /// Applies to the operation's root selection only, not to nested fields.
    /// </remarks>
    TSummary Root(double rootTypeWeight, TSummary selection);
}
