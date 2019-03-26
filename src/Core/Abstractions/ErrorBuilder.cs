using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace HotChocolate
{
    public class ErrorBuilder
        : IErrorBuilder
    {
        private readonly Error _error = new Error();

        public ErrorBuilder()
        {
        }

        private ErrorBuilder(IError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            _error.Message = error.Message;
            _error.Code = error.Code;
            _error.Exception = error.Exception;
            if (error.Extensions != null && error.Extensions.Count > 0)
            {
                _error.Extensions = ImmutableDictionary
                    .CreateRange(error.Extensions);
            }
            if (error.Locations != null && error.Locations.Count > 0)
            {
                _error.Locations = ImmutableList.CreateRange(error.Locations);
            }
            _error.Path = error.Path;
        }

        public IErrorBuilder SetMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    "The message mustn't be null or empty.",
                    nameof(message));
            }
            _error.Message = message;
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
            _error.Extensions = _error.Extensions.SetItem(key, value);
            return this;
        }

        public IErrorBuilder RemoveExtension(string key)
        {
            _error.Extensions = _error.Extensions.Remove(key);
            return this;
        }

        public IErrorBuilder ClearExtensions()
        {
            _error.Extensions = _error.Extensions.Clear();
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
    }
}
