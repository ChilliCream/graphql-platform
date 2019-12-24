using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterConvention : IConvention
    {
        NameString GetArgumentName();

        NameString GetArrayFilterPropertyName();

        NameString CreateFieldName(
            FilterFieldDefintion definition,
            FilterOperationKind kind);

        NameString GetFilterTypeName(IDescriptorContext context, Type entityType);

        IEnumerable<TryCreateImplicitFilter> GetImplicitFilterFactories();
    }
}
