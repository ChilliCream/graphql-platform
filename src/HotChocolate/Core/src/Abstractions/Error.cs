using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Xml.Serialization;
using HotChocolate.Execution;
using HotChocolate.Properties;

#nullable  enable

namespace HotChocolate
{
    internal sealed class Error : IError
    {
        private const string _codePropertyName = "code";

        public Error(
            string message,
            string? code,
            Path? path,
            IReadOnlyList<Location>? locations,
            IReadOnlyDictionary<string, object?>? extensions,
            Exception? exception)
        {
            Message = message;
            Code = code;
            Path = path;
            Locations = locations;
            Extensions = extensions;
            Exception = exception;
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

        public IError WithCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException(
                    AbstractionResources.Error_WithCode_Code_Cannot_Be_Empty,
                    nameof(code));
            }

            var extensions = new ExtensionData(Extensions) {[_codePropertyName] = code};
            return new Error(Message, code, Path, Locations, extensions, Exception);
        }

        public IError RemoveCode()
        {
            var extensions = new ExtensionData(Extensions);
            extensions.Remove(_codePropertyName);
            return new Error(Message, null, Path, Locations, extensions, Exception);
        }

        public IError WithPath(Path path)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return new Error(Message, Code, path, Locations, Extensions, Exception);
        }

        public IError WithPath(IReadOnlyList<object> path)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path.Count == 0)
            {
                throw new ArgumentException(
                    AbstractionResources.Error_WithPath_Path_Cannot_Be_Empty,
                    nameof(path));
            }

            Path segment = Path.New((string)path[0]);

            for (var i = 1; i < path.Count; i++)
            {
                segment = path[i] switch
                {
                    string s => segment.Append(s),
                    int n => segment.Append(n),
                    _ => throw new NotSupportedException(
                        AbstractionResources.Error_WithPath_Path_Value_NotSupported)
                };
            }

            return WithPath(segment);
        }

        public IError RemovePath()
        {
            return new Error(Message, Code, null, Locations, Extensions, Exception);
        }

        public IError WithLocations(IReadOnlyList<Location> locations)
        {
            if (locations == null)
            {
                throw new ArgumentNullException(nameof(locations));
            }

            return new Error(Message, Code, Path, locations, Extensions, Exception);
        }

        public IError RemoveLocations()
        {
            return new Error(Message, Code, Path, null, Extensions, Exception);
        }

        public IError WithExtensions(IReadOnlyDictionary<string, object?> extensions)
        {
            if (extensions == null)
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
                ? new ExtensionData(Extensions)
                : new ExtensionData();
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

            var extensions = new ExtensionData(Extensions);
            extensions.Remove(key);

            return extensions.Count == 0
                ? new Error(Message, Code, Path, Locations, null, Exception)
                : new Error(Message, Code, Path, Locations, extensions, Exception);
        }

        public IError WithException(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return new Error(Message, Code, Path, Locations, Extensions, exception);
        }

        public IError RemoveException()
        {
            return new Error(Message, Code, Path, Locations, Extensions, null);
        }
    }
}
