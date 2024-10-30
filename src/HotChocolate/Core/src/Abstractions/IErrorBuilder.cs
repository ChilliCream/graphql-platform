using HotChocolate.Language;

namespace HotChocolate;

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

    IErrorBuilder AddLocation(ISyntaxNode syntaxNode);

    IErrorBuilder SetLocations<T>(IReadOnlyList<T>? syntaxNodes) where T : ISyntaxNode;

    IErrorBuilder ClearLocations();

    IErrorBuilder SetException(Exception? exception);

    IErrorBuilder RemoveException();

    IErrorBuilder SetExtension(string key, object? value);

    IErrorBuilder RemoveExtension(string key);

    IErrorBuilder ClearExtensions();

    IError Build();
}
