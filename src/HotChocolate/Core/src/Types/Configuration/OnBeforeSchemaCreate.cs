using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration
{
    public delegate void OnBeforeSchemaCreate(
        IDescriptorContext descriptorContext,
        ISchemaBuilder schemaBuilder);
}
