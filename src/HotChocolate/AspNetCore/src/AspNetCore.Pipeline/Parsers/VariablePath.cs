using System.Collections;
using HotChocolate.AspNetCore.Utilities;
using static HotChocolate.AspNetCore.Properties.AspNetCorePipelineResources;

namespace HotChocolate.AspNetCore.Parsers;

internal sealed class VariablePath(KeyPathSegment key) : IEnumerable<IVariablePathSegment>
{
    public KeyPathSegment Key { get; } = key;

    public static VariablePath Parse(string s)
    {
        const string variables = nameof(variables);
        var segments = s.Split('.', StringSplitOptions.RemoveEmptyEntries);

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
            var item = segments[i];

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

    public IEnumerator<IVariablePathSegment> GetEnumerator()
    {
        IVariablePathSegment? current = Key;

        while (current is not null)
        {
            yield return current;

            current = current.Next;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
