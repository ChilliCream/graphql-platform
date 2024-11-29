using System.Collections;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Collections;

public sealed class DirectiveCollection(CompositeDirective[] directives) : IEnumerable<CompositeDirective>
{
    public int Count => directives.Length;

    public bool IsReadOnly => false;

    public IEnumerable<CompositeDirective> this[string directiveName]
    {
        get
        {
            return directives.Length != 0
                ? FindDirectives(directives, directiveName)
                : [];
        }
    }

    private static IEnumerable<CompositeDirective> FindDirectives(CompositeDirective[] directives, string name)
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

    public CompositeDirective? FirstOrDefault(string directiveName)
    {
        var directives1 = directives;

        for (var i = 0; i < directives1.Length; i++)
        {
            var directive = directives1[i];

            if (directive.Name.Equals(directiveName, StringComparison.Ordinal))
            {
                return directive;
            }
        }

        return null;
    }

    public bool ContainsName(string directiveName)
        => FirstOrDefault(directiveName) is not null;

    public bool Contains(CompositeDirective item)
        => directives.Contains(item);

    public void CopyTo(CompositeDirective[] array, int arrayIndex)
    {
        foreach (var directive in directives)
        {
            array[arrayIndex++] = directive;
        }
    }

    /// <inheritdoc />
    public IEnumerator<CompositeDirective> GetEnumerator()
        => ((IEnumerable<CompositeDirective>)directives).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public IReadOnlyList<DirectiveNode> ToSyntaxNodes()
    {
        var nodes = new List<DirectiveNode>();

        foreach (var directive in directives)
        {
            nodes.Add(directive.ToSyntaxNode());
        }

        return nodes;
    }

    public static DirectiveCollection Empty { get; } = new([]);
}
