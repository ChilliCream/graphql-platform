using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting;

[Obsolete("Use HotChocolate.Data.")]
public abstract class SortingNamingConventionBase : ISortingNamingConvention
{
    public virtual string SortKindAscName => "ASC";

    public virtual string SortKindDescName => "DESC";

    public abstract string ArgumentName { get; }

    public virtual string GetSortingTypeName(IDescriptorContext context, Type entityType)
    {
        return context.Naming.GetTypeName(entityType, TypeKind.Object) + "Sort";
    }

    public virtual string GetSortingOperationKindTypeName(
        IDescriptorContext context,
        Type entityType)
    {
        return context.Naming.GetTypeName(entityType, TypeKind.Enum);
    }

    public static ISortingNamingConvention Default { get; } =
        new SortingNamingConventionSnakeCase();

    public string? Scope { get; }
}
