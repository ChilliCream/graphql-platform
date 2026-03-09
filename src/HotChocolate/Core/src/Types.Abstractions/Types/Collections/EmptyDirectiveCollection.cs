using System.Collections;

#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130 // Namespace does not match folder structure

internal sealed class EmptyDirectiveCollection : IReadOnlyDirectiveCollection
{
    private EmptyDirectiveCollection() { }

    public int Count => 0;

    public IEnumerable<IDirective> this[string directiveName] => [];

    public IDirective this[int index] => throw new ArgumentOutOfRangeException(nameof(index));

    public IDirective? FirstOrDefault(string directiveName) => null;

    public IDirective? FirstOrDefault(Type runtimeType) => null;

    public bool ContainsName(string directiveName) => false;

    public IEnumerator<IDirective> GetEnumerator()
    {
        yield break;
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public static EmptyDirectiveCollection Instance { get; } = new();
}
