using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate;

public interface IReadOnlyDirectiveDefinitionCollection : IEnumerable<IDirectiveDefinition>
{
    IDirectiveDefinition this[string name] { get; }

    bool TryGetDirective(string name, [NotNullWhen(true)] out IDirectiveDefinition? definition);

    bool ContainsName(string name);
}
