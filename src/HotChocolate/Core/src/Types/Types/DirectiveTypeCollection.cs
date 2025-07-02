using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HotChocolate.Types;

public sealed class DirectiveTypeCollection : IReadOnlyList<DirectiveType>
{
    private readonly DirectiveType[] _directives;
    private readonly FrozenDictionary<string, DirectiveType> _directiveLookup;
    private ReadOnlyDirectiveDefinitionCollection? _wrapper;

    public DirectiveTypeCollection(DirectiveType[] directives)
    {
        _directives = directives ?? throw new ArgumentNullException(nameof(directives));
        _directiveLookup = directives.ToFrozenDictionary(t => t.Name, StringComparer.Ordinal);
    }

    public int Count => _directives.Length;

    public DirectiveType this[int index] => _directives[index];

    /// <summary>
    /// Gets a directive type by its name.
    /// </summary>
    /// <param name="name">
    /// The directive name.
    /// </param>
    /// <returns>
    /// Returns directive type resolved by the given name
    /// or <c>null</c> if there is no directive with the specified name.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The specified directive type does not exist.
    /// </exception>
    public DirectiveType this[string name] => _directiveLookup[name];

    public bool TryGetDirective(string name, [NotNullWhen(true)] out DirectiveType? directive)
        => _directiveLookup.TryGetValue(name, out directive);

    public bool ContainsName(string name) => _directiveLookup.ContainsKey(name);

    public IEnumerator<DirectiveType> GetEnumerator()
        => Unsafe.As<IEnumerable<DirectiveType>>(_directives).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IReadOnlyDirectiveDefinitionCollection AsReadOnlyDirectiveCollection()
        => _wrapper ??= new ReadOnlyDirectiveDefinitionCollection(this);

    private sealed class ReadOnlyDirectiveDefinitionCollection(DirectiveTypeCollection directives) : IReadOnlyDirectiveDefinitionCollection
    {
        public int Count => directives._directives.Length;

        public IDirectiveDefinition this[int index] => directives._directives[index];

        public IDirectiveDefinition this[string name] => directives._directiveLookup[name];

        public bool TryGetDirective(string name, [NotNullWhen(true)] out IDirectiveDefinition? directive)
        {
            if (directives._directiveLookup.TryGetValue(name, out var directiveType))
            {
                directive = directiveType;
                return true;
            }

            directive = null;
            return false;
        }

        public bool ContainsName(string name) => directives._directiveLookup.ContainsKey(name);

        public IEnumerator<IDirectiveDefinition> GetEnumerator()
            => directives.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
