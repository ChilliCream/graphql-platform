using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace HotChocolate
{
    internal class Error
        : IError
    {
        private const string _code = "code";
        private IImmutableDictionary<string, object> _extensions =
            ImmutableDictionary<string, object>.Empty;

        private string _message;

        public Error()
        {
            Locations = ImmutableList<Location>.Empty;
        }

        public string Message
        {
            get => _message;
            internal set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(
                        "The message mustn't be null or empty.",
                        nameof(value));
                }
                _message = value;
            }
        }

        public string Code
        {
            get
            {
                if (_extensions.TryGetValue(_code, out object code) &&
                    code is string s)
                {
                    return s;
                }

                return null;
            }
            internal set
            {
                _extensions = (value == null)
                    ? _extensions.Remove(_code)
                    : _extensions.SetItem(_code, value);
            }
        }

        public IReadOnlyList<object> Path { get; set; }

        IReadOnlyList<Location> IError.Locations =>
            Locations.Count == 0 ? null : Locations;

        public IImmutableList<Location> Locations { get; set; }

        public Exception Exception { get; set; }

        IReadOnlyDictionary<string, object> IError.Extensions =>
            Extensions.Count == 0 ? null : Extensions;

        public IImmutableDictionary<string, object> Extensions
        {
            get => _extensions;
            internal set => _extensions = value;
        }

        public IError WithCode(string code)
        {
            Error error = Copy();
            error.Code = code;
            return error;
        }

        public IError WithException(Exception exception)
        {
            Error error = Copy();
            error.Exception = exception;
            return error;
        }

        public IError WithExtensions(
            IReadOnlyDictionary<string, object> extensions)
        {
            Error error = Copy();
            error.Extensions = ImmutableDictionary.CreateRange(extensions);
            return error;
        }

        public IError AddExtension(string key, object value)
        {
            Error error = Copy();
            error.Extensions = error.Extensions.Add(key, value);
            return error;
        }

        public IError RemoveExtension(string key)
        {
            Error error = Copy();
            error.Extensions = error.Extensions.Remove(key);
            return error;
        }


        public IError WithLocations(IReadOnlyList<Location> locations)
        {
            Error error = Copy();
            error.Locations = ImmutableList.CreateRange(locations);
            return error;
        }

        public IError WithMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    "The message mustn't be null or empty.",
                    nameof(message));
            }

            Error error = Copy();
            error.Message = message;
            return error;
        }

        public IError WithPath(Path path)
        {
            Error error = Copy();
            error.Path = path.ToCollection();
            return error;
        }

        public IError WithPath(IReadOnlyList<object> path)
        {
            Error error = Copy();
            error.Path = path;
            return error;
        }

        internal Error Copy()
        {
            return new Error
            {
                Message = Message,
                _extensions = _extensions,
                Path = Path,
                Locations = Locations,
                Exception = Exception
            };
        }
    }
}
