namespace HotChocolate.Types.Descriptors.Definitions;

public interface IDefinitionFactory<out T>
    : IDefinitionFactory
    where T : TypeSystemConfiguration
{
    new T CreateDefinition();
}
