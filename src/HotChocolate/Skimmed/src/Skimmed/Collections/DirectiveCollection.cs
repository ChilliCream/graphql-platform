using System.Collections;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a collection of directives.
/// </summary>
public sealed class DirectiveCollection : IDirectiveCollection
{
    private readonly List<Directive> _directives = [];

    public int Count => _directives.Count;

    public bool IsReadOnly => false;

    public IEnumerable<Directive> this[string directiveName]
    {
        get
        {
            return _directives.Count != 0
                ? FindDirectives(_directives, directiveName)
                : [];
        }
    }

    private static IEnumerable<Directive> FindDirectives(List<Directive> directives, string name)
    {
        for (var i = 0; i < directives.Count; i++)
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
        var directives = _directives;

        for (var i = 0; i < directives.Count; i++)
        {
            var directive = directives[i];

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
        => _directives.Contains(item);

    public void Add(Directive item)
        => _directives.Add(item);

    public bool Replace(Directive currentDirective, Directive newDirective)
    {
        for (var i = 0; i < _directives.Count; i++)
        {
            if (ReferenceEquals(_directives[i], currentDirective))
            {
                _directives[i] = newDirective;
                return true;
            }
        }

        return false;
    }

    public bool Remove(Directive item)
        => _directives.Remove(item);

    public void Clear()
        => _directives.Clear();

    public void CopyTo(Directive[] array, int arrayIndex)
    {
        foreach (var directive in _directives)
        {
            array[arrayIndex++] = directive;
        }
    }

    /// <inheritdoc />
    public IEnumerator<Directive> GetEnumerator()
        => ((IEnumerable<Directive>)_directives).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
