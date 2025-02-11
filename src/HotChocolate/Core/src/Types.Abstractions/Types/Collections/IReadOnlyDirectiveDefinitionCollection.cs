using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types;

public interface IReadOnlyDirectiveDefinitionCollection : IEnumerable<IDirectiveDefinition>
{
    IDirectiveDefinition this[string name] { get; }

    bool TryGetDirective(string name, [NotNullWhen(true)] out IDirectiveDefinition? definition);

    bool ContainsName(string name);
}
