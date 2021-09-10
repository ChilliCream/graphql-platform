using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Execution.Processing
{
    /// <summary>
    /// A helper to build the GraphQL result.
    /// </summary>
    internal interface IResultHelper
    {
        /// <summary>
        /// Rent a list for result maps.
        /// </summary>
        ResultMapList RentResultMapList();

        /// <summary>
        /// Rent a result maps.
        /// </summary>
        ResultMap RentResultMap(int count);

        /// <summary>
        /// Rent a list for results.
        /// </summary>
        ResultList RentResultList();

        /// <summary>
        /// Gets the errors of the current result.
        /// </summary>
        IReadOnlyList<IError> Errors { get; }

        /// <summary>
        /// Sets the root result map for the current result.
        /// </summary>
        void SetData(ResultMap resultMap);

        /// <summary>
        /// Sets an extension entry for the current result.
        /// </summary>
        void SetExtension(string key, object? value);

        /// <summary>
        /// Sets a context data entry for the current result.
        /// </summary>
        void SetContextData(string key, object? value);

        /// <summary>
        /// Sets the path property for the current result.
        /// </summary>
        /// <param name="path"></param>
        void SetPath(Path? path);

        /// <summary>
        /// Sets a label for the current result.
        /// </summary>
        void SetLabel(string? label);

        /// <summary>
        /// Sets a boolean that defines if this is the final result.
        /// </summary>
        void SetHasNext(bool value);

        /// <summary>
        /// Adds an error thread-safe to the result object.
        /// </summary>
        /// <param name="error">The error that shall be added.</param>
        /// <param name="selection">The affected field.</param>
        void AddError(IError error, FieldNode? selection = null);

        /// <summary>
        /// Adds a errors thread-safe to the result object.
        /// </summary>
        /// <param name="errors">The errors that shall be added.</param>
        /// <param name="selection">The affected field.</param>
        void AddErrors(IEnumerable<IError> errors, FieldNode? selection = null);

        /// <summary>
        /// Adds a non-null violation to the result.
        /// </summary>
        /// <param name="selection">The affected field.</param>
        /// <param name="path">The field path.</param>
        /// <param name="parent">The parent result map.</param>
        void AddNonNullViolation(FieldNode selection, Path path, IResultMap parent);

        /// <summary>
        /// Builds the final result.
        /// </summary>
        IQueryResult BuildResult();

        void DropResult();

        void Clear();
    }
}
