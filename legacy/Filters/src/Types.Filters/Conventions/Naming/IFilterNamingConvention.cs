using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters;

[Obsolete("Use HotChocolate.Data.")]
public interface IFilterNamingConvention : IConvention
{
    string ArgumentName { get; }

    string ArrayFilterPropertyName { get; }

    string CreateFieldName(FilterFieldDefintion definition, FilterOperationKind kind);

    string GetFilterTypeName(IDescriptorContext context, Type entityType);
}
