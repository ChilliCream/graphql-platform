using System;
using System.Collections.Generic;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate
{
    public interface IErrorBuilder
    {
        IErrorBuilder SetMessage(string message);

        IErrorBuilder SetCode(string? code);

        IErrorBuilder RemoveCode();

        IErrorBuilder SetPath(IReadOnlyList<object>? path);

        IErrorBuilder SetPath(Path? path);

        IErrorBuilder RemovePath();

        IErrorBuilder AddLocation(Location location);

        IErrorBuilder AddLocation(int line, int column);

        IErrorBuilder SetSyntaxNode(ISyntaxNode? syntaxNode);

        IErrorBuilder ClearLocations();

        IErrorBuilder SetException(Exception? exception);

        IErrorBuilder RemoveException();

        IErrorBuilder SetExtension(string key, object? value);

        IErrorBuilder RemoveExtension(string key);

        IErrorBuilder ClearExtensions();

        IError Build();
    }
}
