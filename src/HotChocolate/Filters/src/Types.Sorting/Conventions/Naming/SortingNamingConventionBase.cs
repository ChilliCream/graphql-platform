using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting
{
    [Obsolete("Use HotChocolate.Data.")]
    public abstract class SortingNamingConventionBase : ISortingNamingConvention
    {
        public virtual NameString SortKindAscName { get; } = "ASC";

        public virtual NameString SortKindDescName { get; } = "DESC";

        public abstract NameString ArgumentName { get; }

        public virtual NameString GetSortingTypeName(IDescriptorContext context, Type entityType)
        {
            return context.Naming.GetTypeName(entityType, TypeKind.Object) + "Sort";
        }

        public virtual NameString GetSortingOperationKindTypeName(
            IDescriptorContext context,
            Type entityType)
        {
            return context.Naming.GetTypeName(entityType, TypeKind.Enum);
        }

        public static ISortingNamingConvention Default { get; } =
            new SortingNamingConventionSnakeCase();

        public string? Scope { get; }
    }
}
