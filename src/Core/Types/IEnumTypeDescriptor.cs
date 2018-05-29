namespace HotChocolate.Types
{
    public interface IEnumTypeDescriptor
    {
        IEnumTypeDescriptor Name(string name);
        IEnumTypeDescriptor Description(string description);
        IEnumValueDescriptor Item<T>(T value);
    }

    public interface IEnumTypeDescriptor<T>
        : IEnumTypeDescriptor
    {
        IEnumValueDescriptor Item(T value);
    }
}
