using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// A pluggable analysis the cost engine evaluates while traversing an
/// operation's condition trees and type regions.
/// </summary>
/// <remarks>
/// Laws every implementation must satisfy: the summary type forms a
/// preorder; <see cref="Combine"/> is a commutative monoid with identity
/// <see cref="Empty"/> and is monotone with respect to the preorder;
/// <see cref="Join"/> is an upper bound of both operands. The ExactCases
/// backend additionally requires sub-distributivity of <see cref="Field"/>
/// and <see cref="Combine"/> over <see cref="Join"/>. Idempotence, a least
/// upper bound and full distributivity are not required.
/// </remarks>
/// <typeparam name="TSummary">
/// The summary value this algebra combines and joins across selection
/// boundaries.
/// </typeparam>
[Experimental(CostExperiments.AnalysisAlgebra)]
public interface IAnalysisAlgebra<TSummary>
{
    /// <summary>
    /// Gets the identity element of the <see cref="Combine"/> monoid.
    /// </summary>
    TSummary Empty { get; }

    /// <summary>
    /// Computes the summary contributed by one collected field group.
    /// </summary>
    /// <param name="group">
    /// The collected field group.
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
}
