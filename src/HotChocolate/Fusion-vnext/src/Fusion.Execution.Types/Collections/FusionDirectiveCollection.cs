using System.Collections;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Collections;

public sealed class FusionDirectiveCollection
    : IReadOnlyDirectiveCollection
    , IReadOnlyList<FusionDirective>
{
    private readonly FusionDirective[] _directives;

    public FusionDirectiveCollection(FusionDirective[] directives)
    {
        ArgumentNullException.ThrowIfNull(directives);
        _directives = directives;
    }

    public IEnumerable<FusionDirective> this[string directiveName]
    {
        get
        {
            return _directives.Length != 0
                ? FindDirectives(_directives, directiveName)
                : [];
        }
    }

    IEnumerable<IDirective> IReadOnlyDirectiveCollection.this[string directiveName]
        => this[directiveName];

    public FusionDirective this[int index]
        => _directives[index];

    IDirective IReadOnlyList<IDirective>.this[int index]
        => this[index];

    public int Count => _directives.Length;

    public FusionDirective? FirstOrDefault(string directiveName)
    {
        for (var i = 0; i < _directives.Length; i++)
        {
            var directive = _directives[i];

            if (directive.Name.Equals(directiveName, StringComparison.Ordinal))
            {
                return directive;
            }
        }

        return null;
    }

    IDirective? IReadOnlyDirectiveCollection.FirstOrDefault(string directiveName)
        => FirstOrDefault(directiveName);

    IDirective? IReadOnlyDirectiveCollection.FirstOrDefault(Type runtimeType)
    {
        foreach (var directive in _directives)
        {
            if (directive.Definition.RuntimeType == runtimeType)
            {
                return directive;
            }
        }

        return null;
    }

    private static IEnumerable<FusionDirective> FindDirectives(
        FusionDirective[] directives,
        string name)
    {
        for (var i = 0; i < directives.Length; i++)
        {
            var directive = directives[i];

            if (directive.Name.Equals(name, StringComparison.Ordinal))
            {
                yield return directive;
            }
        }
    }

    public bool ContainsName(string directiveName)
        => FirstOrDefault(directiveName) is not null;

    public bool Contains(FusionDirective item)
        => _directives.Contains(item);

    public void CopyTo(FusionDirective[] array, int arrayIndex)
    {
        foreach (var directive in _directives)
        {
            array[arrayIndex++] = directive;
        }
    }

    public IEnumerable<FusionDirective> AsEnumerable()
        => _directives;

    /// <inheritdoc />
    public IEnumerator<FusionDirective> GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator<IDirective> IEnumerable<IDirective>.GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => AsEnumerable().GetEnumerator();

    public IReadOnlyList<DirectiveNode> ToSyntaxNodes()
    {
        var nodes = new List<DirectiveNode>();

        foreach (var directive in _directives)
        {
            nodes.Add(directive.ToSyntaxNode());
        }

        return nodes;
    }

    public static FusionDirectiveCollection Empty { get; } = new([]);
}
