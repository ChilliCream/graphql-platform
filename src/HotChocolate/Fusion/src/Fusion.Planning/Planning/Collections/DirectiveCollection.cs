using System.Collections;

namespace HotChocolate.Fusion.Planning.Collections;

public sealed class DirectiveCollection(Directive[] directives) : IEnumerable<Directive>
{
    public int Count => directives.Length;

    public bool IsReadOnly => false;

    public IEnumerable<Directive> this[string directiveName]
    {
        get
        {
            return directives.Length != 0
                ? FindDirectives(directives, directiveName)
                : [];
        }
    }

    private static IEnumerable<Directive> FindDirectives(Directive[] directives, string name)
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

    public Directive? FirstOrDefault(string directiveName)
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

    public bool Contains(Directive item)
        => directives.Contains(item);

    public void CopyTo(Directive[] array, int arrayIndex)
    {
        foreach (var directive in directives)
        {
            array[arrayIndex++] = directive;
        }
    }

    /// <inheritdoc />
    public IEnumerator<Directive> GetEnumerator()
        => ((IEnumerable<Directive>)directives).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
