using System;
using System.Collections.Generic;

namespace HotChocolate
{
    public class ErrorBuilder
        : IErrorBuilder
    {
        private readonly Error _error = new Error();

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

        public IErrorBuilder SetPath(IReadOnlyCollection<string> path)
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
    }
}
