namespace HotChocolate.Configuration
{
    public interface IBindType<T>
        : IBoundType<T>
        where T : class
    {
        IBoundType<T> To(NameString typeName);
    }
}
