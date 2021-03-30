using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration
{
    public delegate void OnAfterSchemaCreate(
        IDescriptorContext descriptorContext,
        ISchema schema);
}
