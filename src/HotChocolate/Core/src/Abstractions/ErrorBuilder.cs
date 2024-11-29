#if NET8_0
using HotChocolate.Execution;
#endif
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate;

public class ErrorBuilder : IErrorBuilder
{
    private string? _message;
    private string? _code;
    private Path? _path;
    private Exception? _exception;
    private OrderedDictionary<string, object?>? _extensions;
    private List<Location>? _locations;
    private bool _dirtyLocation;
    private bool _dirtyExtensions;

    public ErrorBuilder()
    {
    }

    private ErrorBuilder(IError error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        _message = error.Message;
        _code = error.Code;
        _path = error.Path;
        _exception = error.Exception;

        if (error.Extensions is { Count: > 0 })
        {
            _extensions = new OrderedDictionary<string, object?>(error.Extensions);
        }

        if (error.Locations is { Count: > 0 })
        {
            _locations = [..error.Locations,];
        }
    }

    public IErrorBuilder SetMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            throw new ArgumentException(
                AbstractionResources.Error_Message_Must_Not_Be_Null,
                nameof(message));
        }

        _message = message;
        return this;
    }

    public IErrorBuilder SetCode(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return RemoveCode();
        }

        _code = code;
        SetExtension("code", code);
        return this;
    }

    public IErrorBuilder RemoveCode()
    {
        _code = null;
        return this;
    }

    public IErrorBuilder SetPath(Path? path)
    {
        if (path is null)
        {
            return RemovePath();
        }

        _path = path;
        return this;
    }

    public IErrorBuilder SetPath(IReadOnlyList<object>? path) =>
        SetPath(path is null ? null : Path.FromList(path));

    public IErrorBuilder RemovePath()
    {
        _path = null;
        return this;
    }

    public IErrorBuilder AddLocation(Location location)
    {
        if (_dirtyLocation && _locations is not null)
        {
            _locations = [.._locations,];
            _dirtyLocation = false;
        }

        (_locations ??= []).Add(location);
        return this;
    }

    public IErrorBuilder AddLocation(int line, int column) =>
        AddLocation(new Location(line, column));

    public IErrorBuilder AddLocation(ISyntaxNode syntaxNode)
    {
        if (syntaxNode.Location is { } location)
        {
            AddLocation(location.Line, location.Column);
        }

        return this;
    }

    public IErrorBuilder SetLocations<T>(IReadOnlyList<T>? syntaxNodes) where T : ISyntaxNode
    {
        if (syntaxNodes is null)
        {
            _locations = null;
            _dirtyLocation = false;
            return this;
        }

        foreach (var syntaxNode in syntaxNodes)
        {
            if (syntaxNode.Location is { } location)
            {
                AddLocation(location.Line, location.Column);
            }
        }

        return this;
    }

    public IErrorBuilder ClearLocations()
    {
        _dirtyLocation = false;
        _locations = null;
        return this;
    }

    public IErrorBuilder SetExtension(string key, object? value)
    {
        if (_dirtyExtensions && _extensions is { })
        {
            _extensions = new OrderedDictionary<string, object?>(_extensions);
            _dirtyExtensions = false;
        }

        _extensions ??= new OrderedDictionary<string, object?>();
        _extensions[key] = value;
        return this;
    }

    public IErrorBuilder RemoveExtension(string key)
    {
        if (_extensions is null)
        {
            return this;
        }

        if (_dirtyExtensions)
        {
            _extensions = new OrderedDictionary<string, object?>(_extensions);
            _dirtyExtensions = false;
        }

        _extensions.Remove(key);

        if (_extensions.Count == 0)
        {
            _extensions = null;
        }

        return this;
    }

    public IErrorBuilder ClearExtensions()
    {
        _dirtyExtensions = false;
        _extensions = null;
        return this;
    }

    public IErrorBuilder SetException(Exception? exception)
    {
        if (exception is null)
        {
            return RemoveException();
        }

        _exception = exception;
        return this;
    }

    public IErrorBuilder RemoveException()
    {
        _exception = null;
        return this;
    }

    public IError Build()
    {
        if (string.IsNullOrEmpty(_message))
        {
            throw new InvalidOperationException(
                AbstractionResources.Error_Message_Must_Not_Be_Null);
        }

        _dirtyExtensions = true;
        _dirtyLocation = true;

        return new Error(
            _message,
            _code,
            _path,
            _locations,
            _extensions,
            _exception);
    }

    public static ErrorBuilder New() => new();

    public static ErrorBuilder FromError(IError error) => new(error);

    public static ErrorBuilder FromDictionary(IReadOnlyDictionary<string, object?> dict)
    {
        if (dict is null)
        {
            throw new ArgumentNullException(nameof(dict));
        }

        var builder = New();
        builder.SetMessage((string)dict["message"]!);

        if (dict.TryGetValue("extensions", out var obj) &&
            obj is IDictionary<string, object> extensions)
        {
            foreach (var item in extensions)
            {
                builder.SetExtension(item.Key, item.Value);
            }
        }

        if (dict.TryGetValue("path", out obj) && obj is IReadOnlyList<object> path)
        {
            builder.SetPath(path);
        }

        if (dict.TryGetValue("locations", out obj) && obj is IList<object> locations)
        {
            foreach (var loc in locations.OfType<IDictionary<string, object>>())
            {
                builder.AddLocation(new Location(
                    Convert.ToInt32(loc["line"]),
                    Convert.ToInt32(loc["column"])));
            }
        }

        return builder;
    }
}
