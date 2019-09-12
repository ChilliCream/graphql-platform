using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    internal sealed class Error
        : IError
    {
        private string? _message;

        public string Message
        {
            get
            {
                if (_message is null)
                {
                    throw new InvalidOperationException(
                        "Message is a non-null property and is null.");
                }
                return _message;
            }
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

        public string? Code
        {
            get
            {
                if (Extensions != null
                    && Extensions.TryGetValue(ErrorFields.Code, out object? code)
                    && code is string s)
                {
                    return s;
                }

                return null;
            }
        }

        public IReadOnlyList<object>? Path { get; set; }

        public IReadOnlyList<Location>? Locations { get; set; }

        public IReadOnlyDictionary<string, object?>? Extensions { get; set; }

        public Exception? Exception { get; set; }
    }
}
