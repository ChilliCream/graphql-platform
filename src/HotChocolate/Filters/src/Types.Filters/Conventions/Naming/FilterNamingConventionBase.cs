using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters;

[Obsolete("Use HotChocolate.Data.")]
public abstract class FilterNamingConventionBase : IFilterNamingConvention
{
    public abstract string ArgumentName { get; }

    public virtual string ArrayFilterPropertyName => "element";

    public abstract string CreateFieldName(FilterFieldDefintion definition, FilterOperationKind kind);

    public virtual string GetFilterTypeName(IDescriptorContext context, Type entityType)
        => context.Naming.GetTypeName(entityType, TypeKind.Object) + "Filter";

    public static IFilterNamingConvention Default { get; } =
        new FilterNamingConventionSnakeCase();

    public string? Scope { get; }
}
