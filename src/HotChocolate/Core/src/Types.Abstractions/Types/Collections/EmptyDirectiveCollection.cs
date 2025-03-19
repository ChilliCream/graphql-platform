using System.Collections;

namespace HotChocolate.Types;

internal sealed class EmptyDirectiveCollection : IReadOnlyDirectiveCollection
{
    private EmptyDirectiveCollection() { }

    public int Count => 0;

    public IEnumerable<IDirective> this[string directiveName] => [];

    public IDirective this[int index] => throw new ArgumentOutOfRangeException(nameof(index));

    public IDirective? FirstOrDefault(string directiveName) => null;

    public bool ContainsName(string directiveName) => false;

    public IEnumerator<IDirective> GetEnumerator()
    {
        yield break;
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public static EmptyDirectiveCollection Instance { get; } = new();
}
