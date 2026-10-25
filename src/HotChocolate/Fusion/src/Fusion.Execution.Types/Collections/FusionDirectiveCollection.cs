using System.Collections;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Collections;

public sealed class FusionDirectiveCollection
    : IReadOnlyDirectiveCollection
    , IReadOnlyList<FusionDirective>
{
    private readonly FusionDirective[] _directives;
    private readonly int _publicCount;

    public FusionDirectiveCollection(FusionDirective[] directives)
    {
        ArgumentNullException.ThrowIfNull(directives);

        _directives = CreateStablePartition(directives, out _publicCount);
    }

    public IEnumerable<FusionDirective> this[string directiveName]
    {
        get
        {
            return _publicCount != 0
                ? FindDirectives(_directives, _publicCount, directiveName)
                : [];
        }
    }

    IEnumerable<IDirective> IReadOnlyDirectiveCollection.this[string directiveName]
        => this[directiveName];

    public FusionDirective this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_publicCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _directives[index];
        }
    }

    IDirective IReadOnlyList<IDirective>.this[int index]
        => this[index];

    public int Count => _publicCount;

    public AllDirectivesView WithInternals => new(_directives);

    public FusionDirective? FirstOrDefault(string directiveName)
        => FindFirst(_directives, _publicCount, directiveName);

    IDirective? IReadOnlyDirectiveCollection.FirstOrDefault(string directiveName)
        => FirstOrDefault(directiveName);

    IDirective? IReadOnlyDirectiveCollection.FirstOrDefault(Type runtimeType)
        => FindFirst(_directives, _publicCount, runtimeType);

    public bool ContainsName(string directiveName)
        => FirstOrDefault(directiveName) is not null;

    public bool Contains(FusionDirective item)
        => Contains(_directives, _publicCount, item);

    public void CopyTo(FusionDirective[] array, int arrayIndex)
        => CopyTo(_directives, _publicCount, array, arrayIndex);

    public IEnumerable<FusionDirective> AsEnumerable()
        => _publicCount == _directives.Length
            ? _directives
            : EnumerateDirectives(_directives, _publicCount);

    /// <inheritdoc />
    public IEnumerator<FusionDirective> GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator<IDirective> IEnumerable<IDirective>.GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => AsEnumerable().GetEnumerator();

    public IReadOnlyList<DirectiveNode> ToSyntaxNodes()
        => ToSyntaxNodes(_directives, _publicCount);

    private static FusionDirective[] CreateStablePartition(
        FusionDirective[] directives,
        out int publicCount)
    {
        publicCount = 0;
        var foundInternal = false;
        var requiresPartition = false;

        for (var i = 0; i < directives.Length; i++)
        {
            if (directives[i].IsPublic)
            {
                publicCount++;
                requiresPartition |= foundInternal;
            }
            else
            {
                foundInternal = true;
            }
        }

        if (!requiresPartition)
        {
            return directives;
        }

        var partitioned = new FusionDirective[directives.Length];
        var publicIndex = 0;
        var internalIndex = publicCount;

        for (var i = 0; i < directives.Length; i++)
        {
            var directive = directives[i];

            if (directive.IsPublic)
            {
                partitioned[publicIndex++] = directive;
            }
            else
            {
                partitioned[internalIndex++] = directive;
            }
        }

        return partitioned;
    }

    private static FusionDirective? FindFirst(
        FusionDirective[] directives,
        int count,
        string directiveName)
    {
        for (var i = 0; i < count; i++)
        {
            var directive = directives[i];

            if (directive.Name.Equals(directiveName, StringComparison.Ordinal))
            {
                return directive;
            }
        }

        return null;
    }

    private static FusionDirective? FindFirst(
        FusionDirective[] directives,
        int count,
        Type runtimeType)
    {
        for (var i = 0; i < count; i++)
        {
            var directive = directives[i];

            if (directive.Definition.RuntimeType == runtimeType)
            {
                return directive;
            }
        }

        return null;
    }

    private static IEnumerable<FusionDirective> FindDirectives(
        FusionDirective[] directives,
        int count,
        string name)
    {
        for (var i = 0; i < count; i++)
        {
            var directive = directives[i];

            if (directive.Name.Equals(name, StringComparison.Ordinal))
            {
                yield return directive;
            }
        }
    }

    private static bool Contains(
        FusionDirective[] directives,
        int count,
        FusionDirective item)
    {
        for (var i = 0; i < count; i++)
        {
            if (directives[i].Equals(item))
            {
                return true;
            }
        }

        return false;
    }

    private static void CopyTo(
        FusionDirective[] directives,
        int count,
        FusionDirective[] array,
        int arrayIndex)
    {
        for (var i = 0; i < count; i++)
        {
            array[arrayIndex++] = directives[i];
        }
    }

    private static IEnumerable<FusionDirective> EnumerateDirectives(
        FusionDirective[] directives,
        int count)
    {
        for (var i = 0; i < count; i++)
        {
            yield return directives[i];
        }
    }

    private static IReadOnlyList<DirectiveNode> ToSyntaxNodes(
        FusionDirective[] directives,
        int count)
    {
        var nodes = new List<DirectiveNode>(count);

        for (var i = 0; i < count; i++)
        {
            nodes.Add(directives[i].ToSyntaxNode());
        }

        return nodes;
    }

    public static FusionDirectiveCollection Empty { get; } = new([]);

    public readonly struct AllDirectivesView
        : IReadOnlyDirectiveCollection
        , IReadOnlyList<FusionDirective>
    {
        private readonly FusionDirective[]? _directives;

        internal AllDirectivesView(FusionDirective[] directives)
        {
            _directives = directives;
        }

        public IEnumerable<FusionDirective> this[string directiveName]
        {
            get
            {
                var directives = _directives;

                return directives is { Length: > 0 }
                    ? FindDirectives(directives, directives.Length, directiveName)
                    : [];
            }
        }

        IEnumerable<IDirective> IReadOnlyDirectiveCollection.this[string directiveName]
            => this[directiveName];

        public FusionDirective this[int index]
        {
            get
            {
                var directives = _directives;

                if (directives is null || (uint)index >= (uint)directives.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return directives[index];
            }
        }

        IDirective IReadOnlyList<IDirective>.this[int index]
            => this[index];

        public int Count => _directives?.Length ?? 0;

        public FusionDirective? FirstOrDefault(string directiveName)
        {
            var directives = _directives;

            return directives is null
                ? null
                : FindFirst(directives, directives.Length, directiveName);
        }

        IDirective? IReadOnlyDirectiveCollection.FirstOrDefault(string directiveName)
            => FirstOrDefault(directiveName);

        public FusionDirective? FirstOrDefault(Type runtimeType)
        {
            var directives = _directives;

            return directives is null
                ? null
                : FindFirst(directives, directives.Length, runtimeType);
        }

        IDirective? IReadOnlyDirectiveCollection.FirstOrDefault(Type runtimeType)
            => FirstOrDefault(runtimeType);

        public bool ContainsName(string directiveName)
            => FirstOrDefault(directiveName) is not null;

        public bool Contains(FusionDirective item)
        {
            var directives = _directives;

            return directives is not null
                && FusionDirectiveCollection.Contains(directives, directives.Length, item);
        }

        public void CopyTo(FusionDirective[] array, int arrayIndex)
        {
            var directives = _directives;

            if (directives is not null)
            {
                FusionDirectiveCollection.CopyTo(
                    directives,
                    directives.Length,
                    array,
                    arrayIndex);
            }
        }

        public IEnumerable<FusionDirective> AsEnumerable()
            => _directives ?? [];

        public Enumerator GetEnumerator()
            => new(_directives);

        IEnumerator<FusionDirective> IEnumerable<FusionDirective>.GetEnumerator()
            => GetEnumerator();

        IEnumerator<IDirective> IEnumerable<IDirective>.GetEnumerator()
            => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public IReadOnlyList<DirectiveNode> ToSyntaxNodes()
        {
            var directives = _directives;

            return directives is null
                ? []
                : FusionDirectiveCollection.ToSyntaxNodes(directives, directives.Length);
        }

        public struct Enumerator : IEnumerator<FusionDirective>
        {
            private readonly FusionDirective[]? _directives;
            private int _index;

            internal Enumerator(FusionDirective[]? directives)
            {
                _directives = directives;
                _index = -1;
            }

            public readonly FusionDirective Current
            {
                get
                {
                    var directives = _directives;

                    if (directives is null || (uint)_index >= (uint)directives.Length)
                    {
                        throw new InvalidOperationException();
                    }

                    return directives[_index];
                }
            }

            readonly object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                var next = _index + 1;

                if (next < (_directives?.Length ?? 0))
                {
                    _index = next;
                    return true;
                }

                _index = _directives?.Length ?? 0;
                return false;
            }

            public void Reset()
            {
                _index = -1;
            }

            public readonly void Dispose()
            {
            }
        }
    }
}
