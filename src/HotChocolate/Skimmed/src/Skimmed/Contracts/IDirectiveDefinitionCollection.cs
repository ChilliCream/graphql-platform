using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public interface IDirectiveDefinitionCollection : ICollection<DirectiveDefinition>
{
    DirectiveDefinition this[string name] { get; }
    bool TryGetDirective(string name, [NotNullWhen(true)] out DirectiveDefinition? definition);
    void Insert(int index, DirectiveDefinition definition);

    bool Remove(string name);

    void RemoveAt(int index);

    bool ContainsName(string name);

    int IndexOf(DirectiveDefinition definition);

    int IndexOf(string name);
}
