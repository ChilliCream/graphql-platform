using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// A prepared operations is an already compiled and optimized variant
/// of the operation specified in the query document that was provided
/// in the request.
/// </summary>
public interface IPreparedOperation : IOperation
{
    /// <summary>
    /// Gets the internal unique identifier for this operation.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the prepared root selections for this operation.
    /// </summary>
    /// <returns>
    /// Returns the prepared root selections for this operation.
    /// </returns>
    ISelectionSet RootSelectionSet { get; }

    /// <summary>
    /// Gets all selection variants of this operation.
    /// </summary>
    IReadOnlyList<ISelectionVariants> SelectionVariants { get; }

    /// <summary>
    /// Gets the list of include conditions associated with this operation.
    /// </summary>
    IReadOnlyList<IncludeCondition> IncludeConditions { get; }

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
    ISelectionSet GetSelectionSet(ISelection selection, IObjectType typeContext);

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
    IEnumerable<IObjectType> GetPossibleTypes(ISelection selection);

    long CreateIncludeContext(IVariableValueCollection variables);

    /// <summary>
    /// Prints the prepared operation.
    /// </summary>
    string Print();
}
