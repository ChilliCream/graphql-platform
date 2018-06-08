using HotChocolate.Configuration;

namespace HotChocolate.Types
{
    public interface IInterfaceTypeDescriptor
        : IFluent
    {
        IInterfaceTypeDescriptor Name(string name);
        IInterfaceTypeDescriptor Description(string description);
        IInterfaceTypeDescriptor ResolveAbstractType(
            ResolveAbstractType resolveAbstractType);
        IInterfaceFieldDescriptor Field(string name);
    }
}
