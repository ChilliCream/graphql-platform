namespace HotChocolate.Configuration.Bindings
{
    public interface IBindField<T>
        where T : class
    {
        IBoundType<T> Name(NameString fieldName);
    }
}
