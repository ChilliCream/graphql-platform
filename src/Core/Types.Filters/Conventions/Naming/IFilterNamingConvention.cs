using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public interface IFilterNamingConvention : IConvention
    { 
        NameString ArgumentName { get; }
        NameString CreateFieldName(FilterFieldDefintion definition, FilterOperationKind kind);

        NameString GetFilterTypeName(IDescriptorContext context, Type entityType); 
    }
}
