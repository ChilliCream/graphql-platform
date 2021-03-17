using System;
using static HotChocolate.AspNetCore.Properties.AspNetCoreResources;

namespace HotChocolate.AspNetCore.Serialization
{
    internal class VariablePath
    {
        public VariablePath(KeyPathSegment key)
        {
            Key = key;
        }

        public KeyPathSegment Key { get; }

        public static VariablePath Parse(string s)
        {
            const string variables = nameof(variables);
            string[] segments = s.Split('.', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length < 2)
            {
                throw ThrowHelper.HttpMultipartMiddleware_InvalidPath(s);
            }

            if (!string.Equals(segments[0], variables, StringComparison.Ordinal))
            {
                throw ThrowHelper.HttpMultipartMiddleware_PathMustStartWithVariable();
            }

            IVariablePathSegment? segment = null;

            for (var i = segments.Length - 1; i >= 0; i--)
            {
                string item = segments[i];

                if (item.Equals(variables, StringComparison.Ordinal))
                {
                    continue;
                }

                segment = int.TryParse(item, out var index)
                    ? new IndexPathSegment(index, segment)
                    : new KeyPathSegment(item, segment);
            }

            if (segment is KeyPathSegment key)
            {
                return new VariablePath(key);
            }

            throw new InvalidOperationException(VariablePath_Parse_FirstSegmentMustBeKey);
        }
    }
}
