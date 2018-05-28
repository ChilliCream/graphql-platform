namespace HotChocolate.Types
{
    public interface IInterfaceTypeDescriptor
    {
        IInterfaceTypeDescriptor Name(string name);
        IInterfaceTypeDescriptor Description(string description);
        IInterfaceTypeDescriptor ResolveAbstractType(
            ResolveAbstractType resolveAbstractType);
        IFieldDescriptor Field(string name);
    }

}
