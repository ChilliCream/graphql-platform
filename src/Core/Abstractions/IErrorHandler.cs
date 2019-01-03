using System;
using System.Collections.Generic;

namespace HotChocolate
{
    public interface IErrorHandler
    {
        IError Handle(IError error);

        IEnumerable<IError> Handle(IEnumerable<IError> errors);

        IError Handle(Exception exception, Func<IError, IError> configure);
    }
}
