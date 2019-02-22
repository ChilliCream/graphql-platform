namespace HotChocolate.Types.Descriptors
{
    public interface IDescriptionFactory<out T>
        : IDescriptionFactory
        where T : DescriptionBase
    {
        new T CreateDescription();
    }
}
