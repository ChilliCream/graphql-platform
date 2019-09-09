using System;
using System.Collections.Generic;
using System.Linq;

namespace StrawberryShake
{
    public class ErrorBuilder
    {
        private Error _error = new Error();

        public List<Location> _locations;

        public Dictionary<string, object> _extensions;

        private bool _dirty = false;

        public ErrorBuilder()
        {
        }

        private ErrorBuilder(IError source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            CopyError(source);
        }

        public ErrorBuilder SetMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    "The message mustn't be null or empty.",
                    nameof(message));
            }

            CheckIfDirty();
            _error.Message = message;
            return this;
        }

        public ErrorBuilder SetCode(string code)
        {
            return code is null
                ? RemoveExtension(ErrorFields.Code)
                : SetExtension(ErrorFields.Code, code);
        }

        public ErrorBuilder SetPath(IReadOnlyList<object> path)
        {
            CheckIfDirty();
            _error.Path = path;
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
            InitializeLocations();
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
            _error.Locations = null;
            return this;
        }


        public ErrorBuilder SetException(Exception exception)
        {
            CheckIfDirty();
            _error.Exception = exception;
            return this;
        }

        public ErrorBuilder SetExtension(string key, object value)
        {
            CheckIfDirty();
            InitializeExtensions();
            _extensions[key] = value;
            return this;
        }

        public ErrorBuilder RemoveExtension(string key)
        {
            CheckIfDirty();
            InitializeExtensions();
            _extensions.Remove(key);
            return this;
        }

        public ErrorBuilder ClearExtensions()
        {
            CheckIfDirty();
            _extensions = null;
            _error.Extensions = null;
            return this;
        }

        public IError Build()
        {
            if (string.IsNullOrEmpty(_error.Message))
            {
                throw new InvalidOperationException(
                    "The message mustn't be null or empty.");
            }

            _dirty = true;
            return _error;
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
            builder.SetMessage((string)dict[ErrorFields.Message]);

            if (dict.TryGetValue(ErrorFields.Extensions, out object obj)
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

        private void InitializeLocations()
        {
            if (_locations is null)
            {
                _locations = _error.Locations is null
                    ? new List<Location>()
                    : new List<Location>(_error.Locations);
                _error.Locations = _locations;
            }
        }

        private void InitializeExtensions()
        {
            if (_extensions is null)
            {
                _extensions = _error.Extensions is null
                    ? new Dictionary<string, object>()
                    : _error.Extensions.ToDictionary(t => t.Key, t => t.Value);
                _error.Extensions = _extensions;
            }
        }

        private void CheckIfDirty()
        {
            if (_dirty)
            {
                Error source = _error;
                _error = new Error();
                CopyError(source);
                _dirty = false;
            }
        }

        private void CopyError(IError source)
        {
            _locations = null;
            _extensions = null;

            _error.Message = source.Message;
            _error.Exception = source.Exception;

            if (source.Path != null && source.Path.Count > 0)
            {
                _error.Path = source.Path;
            }

            if (source.Locations != null && source.Locations.Count > 0)
            {
                _error.Locations = source.Locations;
            }

            if (source.Extensions != null && source.Extensions.Count > 0)
            {
                _error.Extensions = source.Extensions;
            }
        }
    }
}
