using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters;

internal class FilteringSchemaTypeReferenceConfiguration<TSchemaType>
    : FilteringTypeReferenceConfiguration
{
    public override bool CanHandle(TypeReference typeReference)
        => typeReference is SchemaTypeReference { Type: TSchemaType };

    public FilteringSchemaTypeReferenceConfiguration(ConfigureFilterInputType configure)
        : base(configure)
    {
    }
}
