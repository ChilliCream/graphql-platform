namespace HotChocolate.Types
{
    public interface IEnumTypeDescriptor
    {
        IEnumTypeDescriptor Name(string name);
        IEnumTypeDescriptor Description(string description);
        IEnumTypeDescriptor Item(string name);
    }

    public interface IEnumTypeDescriptor<T>
        : IEnumTypeDescriptor
    {

        IEnumTypeDescriptor<T> Item(T value);
        IEnumTypeDescriptor<T> Item(string name, T value);
    }
}
