using System;
using System.Collections.Generic;

namespace HotChocolate
{
    /// <summary>
    /// An error filter can handle and rewrite errors that occured
    /// during execution.
    /// </summary>
    public interface IErrorFilter
    {
        /// <summary>
        /// OnError is called whenever an error occured during
        /// execution of a query.
        /// </summary>
        /// <param name="error">
        /// The error that occured. This argument cannot be null.
        /// </param>
        /// <param name="exception">
        /// The exception that caused this error.
        /// This parameter might be null when for example
        /// a validation error occured.
        /// </param>
        /// <returns>
        /// Returns the error passed in to this filter or a rewritten error.
        /// It is not allowed to return null.
        /// </returns>
        IError OnError(IError error, Exception exception);
    }
}
