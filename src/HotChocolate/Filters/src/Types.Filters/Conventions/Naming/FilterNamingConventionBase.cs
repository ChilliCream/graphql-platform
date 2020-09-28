using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public abstract class FilterNamingConventionBase : IFilterNamingConvention
    {
        public abstract NameString ArgumentName { get; }

        public virtual NameString ArrayFilterPropertyName => "element";

        public abstract NameString CreateFieldName(FilterFieldDefintion definition, FilterOperationKind kind);

        public virtual NameString GetFilterTypeName(IDescriptorContext context, Type entityType)
        {
            return context.Naming.GetTypeName(
                    entityType, TypeKind.Object) + "Filter";
        }

        public static IFilterNamingConvention Default { get; } =
            new FilterNamingConventionSnakeCase();

        public string? Scope { get; }
    }
}
