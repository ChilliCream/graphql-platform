namespace HotChocolate.Types;

public interface IReadOnlyEnumTypeDefinition : IReadOnlyNamedTypeDefinition
{
    IReadOnlyEnumValueCollection Values { get; }
}
