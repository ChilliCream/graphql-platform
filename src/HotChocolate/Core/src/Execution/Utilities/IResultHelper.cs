using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Execution.Utilities
{
    internal interface IResultHelper
    {
        ResultMapList RentResultMapList();

        ResultMap RentResultMap(int count);

        ResultList RentResultList();

        IReadOnlyList<IError> Errors { get; }

        void SetData(ResultMap resultMap);

        /// <summary>
        /// Adds an error thread-safe to the result object.
        /// </summary>
        /// <param name="error">The error that shall be added.</param>
        void AddError(IError error, FieldNode? selection = null);

        /// <summary>
        /// Adds a errors thread-safe to the result object.
        /// </summary>
        /// <param name="error">The error that shall be added.</param>
        void AddErrors(IEnumerable<IError> errors, FieldNode? selection = null);

        void AddNonNullViolation(FieldNode selection, Path path, IResultMap parent);

        IReadOnlyQueryResult BuildResult();

        void DropResult();
    }
}