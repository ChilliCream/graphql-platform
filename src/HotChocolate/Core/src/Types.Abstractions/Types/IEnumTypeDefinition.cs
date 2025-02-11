namespace HotChocolate.Types;

public interface IEnumTypeDefinition : INamedTypeDefinition
{
    IReadOnlyEnumValueCollection Values { get; }
}
