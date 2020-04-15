using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting.Conventions
{
    public static class SortingConventionDefaults
    {
        public static NameString TypeName(IDescriptorContext context, Type entityType)
            => context.Naming.GetTypeName(entityType, TypeKind.Object) + "Sort";

        public static NameString OperationKindTypeName(
            IDescriptorContext context,
            Type entityType)
                => context.Naming.GetTypeName(entityType, TypeKind.Enum);

        public static string Description(
            IDescriptorContext context,
            Type entityType)
                => context.Naming.GetTypeDescription(entityType, TypeKind.Object);
    }
}
