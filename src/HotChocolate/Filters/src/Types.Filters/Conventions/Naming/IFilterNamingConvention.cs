using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public interface IFilterNamingConvention : IConvention
    {
        NameString ArgumentName { get; }

        NameString ArrayFilterPropertyName { get; }

        NameString CreateFieldName(FilterFieldDefintion definition, FilterOperationKind kind);

        NameString GetFilterTypeName(IDescriptorContext context, Type entityType);
    }
}
