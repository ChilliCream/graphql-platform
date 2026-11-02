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
        var length = s.Length;
        var remaining = s.AsSpan(0, length);
        ReadOnlySpan<char> first = default;
        var count = 0;

        while (!remaining.IsEmpty && count < 2)
        {
            var separator = remaining.IndexOf('.');
            var item = separator < 0 ? remaining : remaining[..separator];
            remaining = separator < 0 ? default : remaining[(separator + 1)..];

            if (item.IsEmpty)
            {
                continue;
            }

            if (count == 0)
            {
                first = item;
            }

            count++;
        }

        if (count < 2)
        {
            throw ThrowHelper.HttpMultipartMiddleware_InvalidPath(s);
        }

        if (!first.Equals(variables, StringComparison.Ordinal))
        {
            throw ThrowHelper.HttpMultipartMiddleware_PathMustStartWithVariable();
        }

        IVariablePathSegment? segment = null;
        remaining = s.AsSpan();

        while (!remaining.IsEmpty)
        {
            var separator = remaining.LastIndexOf('.');
            var item = remaining[(separator + 1)..];
            remaining = separator < 0 ? default : remaining[..separator];

            if (item.IsEmpty || item.Equals(variables, StringComparison.Ordinal))
            {
                continue;
            }

            segment = int.TryParse(item, out var index)
                ? new IndexPathSegment(index, segment)
                : new KeyPathSegment(item.ToString(), segment);
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
