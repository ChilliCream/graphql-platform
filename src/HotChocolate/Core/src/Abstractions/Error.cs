using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Execution;

namespace HotChocolate
{
    internal class Error
        : IError
    {
        private const string _code = "code";
        private OrderedDictionary<string, object> _extensions =
            new OrderedDictionary<string, object>();
        private bool _needsCopy;

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
                CopyExtensions();
                if (value == null)
                {
                    _extensions.Remove(_code);
                }
                else
                {
                    _extensions[_code] = value;
                }
            }
        }

        public IReadOnlyList<object> Path { get; set; }

        IReadOnlyList<Location> IError.Locations =>
            Locations.Count == 0 ? null : Locations;

        public IImmutableList<Location> Locations { get; set; }

        public Exception Exception { get; set; }

        IReadOnlyDictionary<string, object> IError.Extensions =>
            _extensions.Count == 0 ? null : _extensions;

        public OrderedDictionary<string, object> Extensions
        {
            get
            {
                CopyExtensions();
                return _extensions;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _needsCopy = false;
                _extensions = value;
            }
        }

        private void CopyExtensions()
        {
            if (_needsCopy)
            {
                _extensions = _extensions.Clone();
                _needsCopy = false;
            }
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

        public IError RemoveException()
        {
            Error error = Copy();
            error.Exception = null;
            return error;
        }

        public IError WithExtensions(
            IReadOnlyDictionary<string, object> extensions)
        {
            Error error = Copy();
            error.Extensions =
                new OrderedDictionary<string, object>(extensions);
            return error;
        }

        public IError AddExtension(string key, object value)
        {
            Error error = Copy();
            error.Extensions.Add(key, value);
            return error;
        }

        public IError RemoveExtension(string key)
        {
            Error error = Copy();
            error.Extensions.Remove(key);
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
                _needsCopy = true,
                Path = Path,
                Locations = Locations,
                Exception = Exception
            };
        }
    }
}
