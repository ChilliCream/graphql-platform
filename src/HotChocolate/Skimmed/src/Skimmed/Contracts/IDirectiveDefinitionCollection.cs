using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public interface IDirectiveDefinitionCollection : ICollection<DirectiveDefinition>
{
    DirectiveDefinition this[string name] { get; }
    bool TryGetDirective(string name, [NotNullWhen(true)] out DirectiveDefinition? type);
    bool ContainsName(string name);
}
