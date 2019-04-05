using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IDescriptor<T>
        where T : DefinitionBase
    {
        IDescriptorExtension<T> Extend();
    }
}
