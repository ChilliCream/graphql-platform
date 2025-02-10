using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate;

public interface IReadOnlySchemaDefinition
{
    public IReadOnlyObjectTypeDefinition? QueryType { get; }

    public IReadOnlyObjectTypeDefinition? MutationType { get; }

    public IReadOnlyObjectTypeDefinition? SubscriptionType { get; }

    public IReadOnlyDirectiveCollection Directives { get; }

    public IReadOnlyTypeDefinitionCollection Types { get; }

    public IReadOnlyDirectiveDefinitionCollection DirectiveDefinitions { get; }

    public IReadOnlyObjectTypeDefinition GetOperationType(OperationType operationType);
}

public interface IReadOnlyTypeDefinitionCollection : IEnumerable<IReadOnlyNamedTypeDefinition>
{
    IReadOnlyNamedTypeDefinition this[string name] { get; }

    bool TryGetType(string name, [NotNullWhen(true)] out IReadOnlyNamedTypeDefinition? definition);

    bool TryGetType<T>(string name, [NotNullWhen(true)] out T? type) where T : IReadOnlyNamedTypeDefinition;

    bool ContainsName(string name);
}

public interface IReadOnlyDirectiveDefinitionCollection : IEnumerable<IReadOnlyDirectiveDefinition>
{
    IReadOnlyDirectiveDefinition this[string name] { get; }

    bool TryGetDirective(string name, [NotNullWhen(true)] out IReadOnlyDirectiveDefinition? definition);

    bool ContainsName(string name);
}
