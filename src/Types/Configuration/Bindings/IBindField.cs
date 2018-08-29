namespace HotChocolate.Configuration
{
    public interface IBindField<T>
        where T : class
    {
        IBoundType<T> Name(string fieldName);
    }
}
