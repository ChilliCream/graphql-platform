using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StrawberryShake
{
    public class ErrorBuilder
    {
        private string? _message;
        private IReadOnlyList<object>? _path;
        private List<Location>? _locations;
        private Dictionary<string, object?>? _extensions;
        private Exception? _exception;
        private bool _dirty;

        public ErrorBuilder()
        {
        }

        private ErrorBuilder(IError source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _message = source.Message;
            _exception = source.Exception;

            if (source.Path != null && source.Path.Count > 0)
            {
                _path = source.Path;
            }

            if (source.Locations != null && source.Locations.Count > 0)
            {
                _locations = new List<Location>(source.Locations);
            }

            if (source.Extensions != null && source.Extensions.Count > 0)
            {
                _extensions = source.Extensions.ToDictionary(
                    t => t.Key,
                    t => t.Value);
            }
        }

        public ErrorBuilder SetMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    "The message mustn't be null or empty.",
                    nameof(message));
            }

            _message = message;
            return this;
        }

        public ErrorBuilder SetCode(string? code)
        {
            return code is null
                ? RemoveExtension(ErrorFields.Code)
                : SetExtension(ErrorFields.Code, code);
        }

        public ErrorBuilder SetPath(IReadOnlyList<object>? path)
        {
            _path = path;
            return this;
        }

        public ErrorBuilder AddLocations(IEnumerable<Location> locations)
        {
            if (locations is null)
            {
                throw new ArgumentNullException(nameof(locations));
            }

            CheckIfDirty();

            _locations = new List<Location>(locations);

            return this;
        }

        public ErrorBuilder AddLocation(Location location)
        {
            if (location.Equals(default(Location)))
            {
                throw new ArgumentException(
                    "Empty locations are not allowed.",
                    nameof(location));
            }

            CheckIfDirty();

            if (_locations is null)
            {
                _locations = new List<Location>();
            }
            _locations.Add(location);

            return this;
        }

        public ErrorBuilder AddLocation(int line, int column)
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

            return AddLocation(new Location(line, column));
        }

        public ErrorBuilder ClearLocations()
        {
            CheckIfDirty();
            _locations = null;
            return this;
        }


        public ErrorBuilder SetException(Exception? exception)
        {
            _exception = exception;
            return this;
        }

        public ErrorBuilder SetExtensions(
            IEnumerable<KeyValuePair<string, object?>> extensions)
        {
            CheckIfDirty();

            if (extensions is null)
            {
                _extensions = null;
            }
            else
            {
                _extensions = extensions.ToDictionary(t => t.Key, t => t.Value);
            }

            return this;
        }

        public ErrorBuilder SetExtension(string key, object? value)
        {
            CheckIfDirty();

            if (_extensions is null)
            {
                _extensions = new Dictionary<string, object?>();
            }
            _extensions[key] = value;

            return this;
        }

        public ErrorBuilder RemoveExtension(string key)
        {
            CheckIfDirty();

            if (_extensions != null)
            {
                _extensions.Remove(key);
            }

            return this;
        }

        public ErrorBuilder ClearExtensions()
        {
            CheckIfDirty();
            _extensions = null;
            return this;
        }

        public IError Build()
        {
            if (string.IsNullOrEmpty(_message))
            {
                throw new InvalidOperationException(
                    "The message mustn't be null or empty.");
            }

            _dirty = true;
            return new Error
            (
                _message!,
                _path,
                _locations,
                _extensions,
                _exception
            );
        }

        public static ErrorBuilder New() => new ErrorBuilder();

        public static ErrorBuilder FromError(IError error)
        {
            return new ErrorBuilder(error);
        }

        public static ErrorBuilder FromException(Exception exception)
        {
            return ErrorBuilder.New()
                .SetMessage(exception.Message)
                .SetException(exception);
        }

        public static ErrorBuilder FromDictionary(
            IReadOnlyDictionary<string, object> dict)
        {
            if (dict == null)
            {
                throw new ArgumentNullException(nameof(dict));
            }

            var builder = ErrorBuilder.New();
            builder.SetMessage((string)dict[ErrorFields.Message]);

            if (dict.TryGetValue(ErrorFields.Extensions, out object? obj)
                && obj is IDictionary<string, object> extensions)
            {
                foreach (var item in extensions)
                {
                    builder.SetExtension(item.Key, item.Value);
                }
            }

            if (dict.TryGetValue(ErrorFields.Path, out obj)
                && obj is IReadOnlyList<object> path)
            {
                builder.SetPath(path);
            }

            if (dict.TryGetValue(ErrorFields.Locations, out obj)
                && obj is IList<object> locations)
            {
                foreach (IDictionary<string, object> loc in locations
                    .OfType<IDictionary<string, object>>())
                {
                    builder.AddLocation(new Location(
                        Convert.ToInt32(loc[ErrorFields.Line]),
                        Convert.ToInt32(loc[ErrorFields.Column])));
                }
            }

            return builder;
        }

        private void CheckIfDirty()
        {
            if (_dirty)
            {
                if (_locations != null)
                {
                    _locations = new List<Location>(_locations);
                }

                if (_extensions != null)
                {
                    _extensions = new Dictionary<string, object?>(_extensions);
                }
                _dirty = false;
            }
        }
    }
}
