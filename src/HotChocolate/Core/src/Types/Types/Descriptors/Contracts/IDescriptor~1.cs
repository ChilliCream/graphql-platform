using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    public interface IDescriptor<T> : IDescriptor where T : DefinitionBase
    {
        IDescriptorExtension<T> Extend();
    }
}
