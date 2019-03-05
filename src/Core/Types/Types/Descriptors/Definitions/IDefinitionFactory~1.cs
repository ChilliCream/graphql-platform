namespace HotChocolate.Types.Descriptors.Definitions
{
    public interface IDefinitionFactory<out T>
        : IDefinitionFactory
        where T : DefinitionBase
    {
        new T CreateDefinition();
    }
}
