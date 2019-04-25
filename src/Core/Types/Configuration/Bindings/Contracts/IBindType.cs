namespace HotChocolate.Configuration.Bindings
{
    public interface IBindType<T>
        : IBoundType<T>
        where T : class
    {
        IBoundType<T> To(NameString typeName);
    }
}
