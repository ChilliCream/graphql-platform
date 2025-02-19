using System.Collections;

namespace HotChocolate.Types.Mutable;

/// <summary>
/// Represents a collection of directives.
/// </summary>
public sealed class DirectiveCollection
    : IList<Directive>
    , IReadOnlyDirectiveCollection
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

    IEnumerable<IDirective> IReadOnlyDirectiveCollection.this[string directiveName]
        => this[directiveName];

    public Directive this[int index]
    {
        get => _directives[index];
        set => _directives[index] = value;
    }

    IDirective IReadOnlyList<IDirective>.this[int index]
        => _directives[index];

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

    IDirective? IReadOnlyDirectiveCollection.FirstOrDefault(string directiveName)
        => FirstOrDefault(directiveName);

    public bool ContainsName(string directiveName)
        => FirstOrDefault(directiveName) is not null;

    public bool Contains(Directive item)
        => _directives.Contains(item);

    public int IndexOf(Directive item)
        => _directives.IndexOf(item);

    public void Add(Directive item)
        => _directives.Add(item);

    public void Insert(int index, Directive item)
        => _directives.Insert(index, item);

    public void RemoveAt(int index)
        => _directives.RemoveAt(index);

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

    public IEnumerable<Directive> AsEnumerable()
        => _directives;

    public IEnumerator<Directive> GetEnumerator()
        => _directives.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    IEnumerator<IDirective> IEnumerable<IDirective>.GetEnumerator()
        => GetEnumerator();
}
