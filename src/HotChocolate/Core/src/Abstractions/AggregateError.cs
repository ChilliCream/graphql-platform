using System.Collections.Generic;
using System.Linq;
using HotChocolate.Properties;

namespace HotChocolate
{
    /// <summary>
    /// An aggregate error allows to pass a collection of error in a single error object.
    /// </summary>
    public class AggregateError : Error
    {
        public AggregateError(IEnumerable<IError> errors)
            : base(AbstractionResources.AggregateError_Message)
        {
            Errors = errors.ToArray();
        }

        public AggregateError(params IError[] errors)
            : base(AbstractionResources.AggregateError_Message)
        {
            Errors = errors.ToArray();
        }

        /// <summary>
        /// Gets the actual errors.
        /// </summary>
        public IReadOnlyList<IError> Errors { get; }
    }
}
