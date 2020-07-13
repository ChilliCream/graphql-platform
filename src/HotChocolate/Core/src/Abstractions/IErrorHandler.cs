using System;

namespace HotChocolate
{
    public interface IErrorHandler
    {
        IError Handle(IError error);

        /// <summary>
        /// Creates an error from an unexpected exception.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        IErrorBuilder CreateUnexpectedError(Exception exception);
    }
}
