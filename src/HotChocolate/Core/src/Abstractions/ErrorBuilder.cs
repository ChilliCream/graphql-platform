using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate
{
    public class ErrorBuilder : IErrorBuilder
    {
        private string? _message;
        private string? _code;
        private Path? _path;
        private Exception? _exception;
        private ExtensionData? _extensions;
        private List<Location>? _locations;

        public ErrorBuilder()
        {
        }

        private ErrorBuilder(IError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            _message = error.Message;
            _code = error.Code;
            _path = error.Path;
            _exception = error.Exception;

            if (error.Extensions is { } && error.Extensions.Count > 0)
            {
                _extensions = new ExtensionData(error.Extensions);
            }

            if (error.Locations is { } && error.Locations.Count > 0)
            {
                _locations = new List<Location>(error.Locations);
            }
        }

        /// <summary>
        /// Gets the error message.
        /// This property is mandatory and cannot be null.
        /// </summary>
        public string? Message => _message;

        /// <summary>
        /// Gets an error code that can be used to automatically
        /// process an error.
        /// This property is optional and can be null.
        /// </summary>
        public string? Code => _code;

        /// <summary>
        /// Gets the path to the object that caused the error.
        /// This property is optional and can be null.
        /// </summary>
        public Path? Path => _path;

        /// <summary>
        /// Gets the source text positions to which this error refers to.
        /// This property is optional and can be null.
        /// </summary>
        public IReadOnlyList<Location>? Locations => _locations;

        /// <summary>
        /// Gets non-spec error properties.
        /// This property is optional and can be null.
        /// </summary>
        public IReadOnlyDictionary<string, object?>? Extensions => _extensions;

        /// <summary>
        /// Gets the exception associated with this error.
        /// </summary>
        public Exception? Exception => _exception;

        public IErrorBuilder SetMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    AbstractionResources.Error_Message_Mustnt_Be_Null,
                    nameof(message));
            }
            _message = message;
            return this;
        }

        public IErrorBuilder SetCode(string code)
        {
            _error.Code = code;
            return this;
        }

        public IErrorBuilder SetPath(IReadOnlyList<object> path)
        {
            _error.Path = path;
            return this;
        }

        public IErrorBuilder SetPath(Path path)
        {
            _error.Path = path?.ToCollection();
            return this;
        }

        public IErrorBuilder AddLocation(Location location)
        {
            _error.Locations = _error.Locations.Add(location);
            return this;
        }

        public IErrorBuilder AddLocation(int line, int column)
        {
            if (line < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(line), line,
                    "line is a 1-base index and cannot be less than one.");
            }

            if (column < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(column), column,
                    "column is a 1-base index and cannot be less than one.");
            }

            _error.Locations = _error.Locations.Add(new Location(line, column));
            return this;
        }

        public IErrorBuilder ClearLocations()
        {
            _error.Locations = _error.Locations.Clear();
            return this;
        }


        public IErrorBuilder SetException(Exception exception)
        {
            _error.Exception = exception;
            return this;
        }

        public IErrorBuilder SetExtension(string key, object value)
        {
            _error.Extensions[key] = value;
            return this;
        }

        public IErrorBuilder RemoveExtension(string key)
        {
            _error.Extensions.Remove(key);
            return this;
        }

        public IErrorBuilder ClearExtensions()
        {
            _error.Extensions.Clear();
            return this;
        }

        public IError Build()
        {
            if (string.IsNullOrEmpty(_error.Message))
            {
                throw new InvalidOperationException(
                    "The message mustn't be null or empty.");
            }
            return _error.Copy();
        }

        public static ErrorBuilder New() => new ErrorBuilder();

        public static ErrorBuilder FromError(IError error)
        {
            return new ErrorBuilder(error);
        }

        public static ErrorBuilder FromDictionary(
            IReadOnlyDictionary<string, object> dict)
        {
            if (dict == null)
            {
                throw new ArgumentNullException(nameof(dict));
            }

            var builder = ErrorBuilder.New();
            builder.SetMessage((string)dict["message"]);

            if (dict.TryGetValue("extensions", out object obj)
                && obj is IDictionary<string, object> extensions)
            {
                foreach (var item in extensions)
                {
                    builder.SetExtension(item.Key, item.Value);
                }
            }

            if (dict.TryGetValue("path", out obj)
                && obj is IReadOnlyList<object> path)
            {
                builder.SetPath(path);
            }

            if (dict.TryGetValue("locations", out obj)
                && obj is IList<object> locations)
            {
                foreach (IDictionary<string, object> loc in locations
                    .OfType<IDictionary<string, object>>())
                {
                    builder.AddLocation(new Location(
                        Convert.ToInt32(loc["line"]),
                        Convert.ToInt32(loc["column"])));
                }
            }

            return builder;
        }
    }
}
