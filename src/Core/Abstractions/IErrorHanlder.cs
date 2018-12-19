using System;

namespace HotChocolate
{
    public interface IErrorHanlder
    {
        IError Handle(IError error);

        IError Handle(Exception exception);
    }
}
