using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.RateLimit
{
    public class RequestIdentity
    {
        public static readonly RequestIdentity Empty =
            new RequestIdentity(string.Empty, new string[0]);

        private readonly string _composedValue;

        private RequestIdentity(string path, params string[] ids)
        {
            Ids = new List<string>(ids).AsReadOnly();
            Path = path;

            _composedValue = $"{string.Join("-", Ids)}-{Path}";
        }

        public IReadOnlyCollection<string> Ids { get; }
        public string Path { get; }
        public bool IsEmpty => Ids.Count < 1;

        public static RequestIdentity Create(string path, params string[] ids)
        {
            if (ids == null || ids.Any(string.IsNullOrEmpty))
            {
                return Empty;
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(path));
            }

            return new RequestIdentity(path, ids);
        }

        public static implicit operator string(RequestIdentity value)
        {
            return value._composedValue;
        }
    }
}
