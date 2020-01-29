using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    public sealed class Error
        : IError
    {
        public Error(
            string message,
            IReadOnlyList<object>? path,
            IReadOnlyList<Location>? locations,
            IReadOnlyDictionary<string, object?>? extensions,
            Exception? exception)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Path = path;
            Locations = locations;
            Extensions = extensions;
            Exception = exception;
        }

        public string Message { get; }

        public string? Code
        {
            get
            {
                if (Extensions is { }
                    && Extensions.TryGetValue("code", out var o)
                    && o is string code)
                {
                    return code;
                }
                return null;
            }
        }

        public IReadOnlyList<object>? Path { get; }

        public IReadOnlyList<Location>? Locations { get; }

        public IReadOnlyDictionary<string, object?>? Extensions { get; }

        public Exception? Exception { get; }
    }
}
