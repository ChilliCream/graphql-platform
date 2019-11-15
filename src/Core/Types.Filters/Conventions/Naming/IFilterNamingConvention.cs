using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public interface IFilterNamingConvention : IConvention
    {
        NameString CreateFieldName(FilterFieldDefintion definition, FilterOperationKind kind);
    }
}
