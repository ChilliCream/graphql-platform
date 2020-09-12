using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate
{
    public sealed class Error : IError
    {
        private const string _codePropertyName = "code";

        public Error(
            string message,
            string? code = null,
            Path? path = null,
            IReadOnlyList<Location>? locations = null,
            IReadOnlyDictionary<string, object?>? extensions = null,
            Exception? exception = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    AbstractionResources.Error_WithMessage_Message_Cannot_Be_Empty,
                    nameof(message));
            }

            Message = message;
            Code = code;
            Path = path;
            Locations = locations;
            Extensions = extensions;
            Exception = exception;

            if (code is not null && Extensions is null)
            {
                Extensions = new OrderedDictionary<string, object?> { { _codePropertyName, code } };
            }
        }

        public string Message { get; }

        public string? Code { get; }

        public Path? Path { get; }

        public IReadOnlyList<Location>? Locations { get; }

        public IReadOnlyDictionary<string, object?>? Extensions { get; }

        public Exception? Exception { get; }

        public IError WithMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    AbstractionResources.Error_WithMessage_Message_Cannot_Be_Empty,
                    nameof(message));
            }

            return new Error(message, Code, Path, Locations, Extensions, Exception);
        }

        public IError WithCode(string? code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return RemoveCode();
            }

            var extensions = Extensions is null
                ? new OrderedDictionary<string, object?>() { [_codePropertyName] = code }
                : new OrderedDictionary<string, object?>(Extensions) { [_codePropertyName] = code };
            return new Error(Message, code, Path, Locations, extensions, Exception);
        }

        public IError RemoveCode()
        {
            IReadOnlyDictionary<string, object?>? extensions = Extensions;

            if (Extensions is { })
            {
                var temp = new OrderedDictionary<string, object?>(Extensions);
                temp.Remove(_codePropertyName);
                extensions = temp;
            }

            return new Error(Message, null, Path, Locations, extensions, Exception);
        }

        public IError WithPath(Path? path)
        {
            return path is null
                ? RemovePath()
                : new Error(Message, Code, path, Locations, Extensions, Exception);
        }

        public IError WithPath(IReadOnlyList<object>? path) =>
            WithPath(path is null ? null : Path.FromList(path));

        public IError RemovePath()
        {
            return new Error(Message, Code, null, Locations, Extensions, Exception);
        }

        public IError WithLocations(IReadOnlyList<Location>? locations)
        {
            return locations is null
                ? RemoveLocations()
                : new Error(Message, Code, Path, locations, Extensions, Exception);
        }

        public IError RemoveLocations()
        {
            return new Error(Message, Code, Path, null, Extensions, Exception);
        }

        public IError WithExtensions(IReadOnlyDictionary<string, object?> extensions)
        {
            if (extensions is null)
            {
                throw new ArgumentNullException(nameof(extensions));
            }

            return new Error(Message, Code, Path, Locations, extensions, Exception);
        }

        public IError RemoveExtensions()
        {
            return new Error(Message, Code, Path, Locations, null, Exception);
        }

        public IError SetExtension(string key, object? value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(
                    AbstractionResources.Error_SetExtension_Key_Cannot_Be_Empty,
                    nameof(key));
            }

            var extensions = Extensions is { }
                ? new OrderedDictionary<string, object?>(Extensions)
                : new OrderedDictionary<string, object?>();
            extensions[key] = value;
            return new Error(Message, Code, Path, Locations, extensions, Exception);
        }

        public IError RemoveExtension(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(
                    AbstractionResources.Error_SetExtension_Key_Cannot_Be_Empty,
                    nameof(key));
            }

            if (Extensions is null)
            {
                return this;
            }

            var extensions = new OrderedDictionary<string, object?>(Extensions);
            extensions.Remove(key);

            return extensions.Count == 0
                ? new Error(Message, Code, Path, Locations, null, Exception)
                : new Error(Message, Code, Path, Locations, extensions, Exception);
        }

        public IError WithException(Exception? exception)
        {
            return exception is null ?
                RemoveException() :
                new Error(Message, Code, Path, Locations, Extensions, exception);
        }

        public IError RemoveException()
        {
            return new Error(Message, Code, Path, Locations, Extensions);
        }
    }
}
