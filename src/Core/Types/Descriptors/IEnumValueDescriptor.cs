using HotChocolate.Configuration;

namespace HotChocolate.Types
{
    public interface IEnumValueDescriptor
        : IFluent
    {
        IEnumValueDescriptor Name(string name);
        IEnumValueDescriptor Description(string description);
        IEnumValueDescriptor DeprecationReason(string deprecationReason);
    }
}
