using System;
using System.Collections.Generic;

namespace HotChocolate
{
    public interface IErrorBuilder
    {
        IErrorBuilder SetMessage(string message);

        IErrorBuilder SetCode(string code);

        IErrorBuilder SetPath(IReadOnlyCollection<string> path);

        IErrorBuilder SetPath(Path path);

        IErrorBuilder AddLocation(Location location);

        IErrorBuilder SetException(Exception exception);

        IErrorBuilder SetExtension(string key, object value);

        IError Build();
    }
}
