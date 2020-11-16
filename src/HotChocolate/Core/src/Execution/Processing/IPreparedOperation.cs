using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
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

        // TODO : we might want to move this into the execution plan.
        int ProposedTaskCount { get; }

        /// <summary>
        /// Gets the prepared root selections for this operation.
        /// </summary>
        /// <returns>
        /// Returns the prepared root selections for this operation.
        /// </returns>
        ISelectionSet GetRootSelectionSet();

        /// <summary>
        /// Gets the prepared selections for the specified
        /// selection set under the specified type context.
        /// </summary>
        /// <param name="selectionSet">
        /// The selection set for which the selections shall be collected.
        /// </param>
        /// <param name="typeContext">
        /// The type context under which the selections shall be collected.
        /// </param>
        /// <returns>
        /// Returns the prepared selections for the specified
        /// selection set under the specified type context.
        /// </returns>
        ISelectionSet GetSelectionSet(
            SelectionSetNode selectionSet,
            IObjectType typeContext);

        /// <summary>
        /// Gets the types that are possible for the specified <paramref name="selectionSet"/>.
        /// </summary>
        /// <param name="selectionSet">
        /// The selection set for which the possible types shall be inspected.
        /// </param>
        /// <returns>
        /// Returns the types that are possible for the specified <paramref name="selectionSet"/>.
        /// </returns>
        IEnumerable<IObjectType> GetPossibleTypes(
            SelectionSetNode selectionSet);

        /// <summary>
        /// Gets all selection variants of this operation.
        /// </summary>
        IEnumerable<ISelectionVariants> SelectionVariants { get; }

        /// <summary>
        /// Prints the prepared operation.
        /// </summary>
        string Print();
    }
}
