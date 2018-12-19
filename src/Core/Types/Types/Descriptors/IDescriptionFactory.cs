namespace HotChocolate.Types
{
    internal interface IDescriptionFactory<out T>
    {
        T CreateDescription();
    }
}
