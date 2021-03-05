using System.Collections.Generic;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters
{
    /// <summary>
    /// A context object that is passed along the visitation cycle
    /// </summary>
    public interface IFilterVisitorContext
        : ISyntaxVisitorContext
    {
        /// <summary>
        /// The already visited types
        /// </summary>
        Stack<IType> Types { get; }

        /// <summary>
        /// The already visited operations
        /// </summary>
        Stack<IInputField> Operations { get; }

        /// <summary>
        /// A list of errors that will be raised once the visitation is finished
        /// </summary>
        IList<IError> Errors { get; }
    }
}
