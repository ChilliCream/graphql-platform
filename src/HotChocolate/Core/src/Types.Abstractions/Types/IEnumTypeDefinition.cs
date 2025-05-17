namespace HotChocolate.Types;

public interface IEnumTypeDefinition : ITypeDefinition
{
    IReadOnlyEnumValueCollection Values { get; }
}
