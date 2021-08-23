using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Execution.Processing
{
    internal interface IResultHelper
    {
        ResultMapList RentResultMapList();

        ResultMap RentResultMap(int count);

        ResultList RentResultList();

        IReadOnlyList<IError> Errors { get; }

        void SetData(ResultMap resultMap);

        void SetExtension(string key, object? value);

        void SetContextData(string key, object? value);

        void SetPath(Path? path);

        void SetLabel(string? label);

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

        void AddNonNullViolation(FieldNode selection, Path path, IResultMap parent);

        IQueryResult BuildResult();

        void DropResult();

        void Clear();
    }
}
